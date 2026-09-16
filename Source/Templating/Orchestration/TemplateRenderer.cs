// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Text.RegularExpressions;
using Cratis.Templating.Configuration;
using Cratis.Templating.Expressions;
using Cratis.Templating.FileSystem;
using Cratis.Templating.Processing;
using Cratis.Templating.Symbols;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Orchestration;

/// <summary>
/// One file produced by a render: its absolute target path and whether it was a byte-exact copy.
/// </summary>
/// <param name="Path">Absolute target path.</param>
/// <param name="WasCopyOnly">Whether the file was copied byte-exact without processing.</param>
public record RenderedFile(string Path, bool WasCopyOnly);

/// <summary>
/// Renders a template's planned files into an output directory: conditional processing per file family,
/// custom operations, token replacement with value forms, GUID replacement, rename rules, the
/// placeholder filename conventions, and byte-exact handling of unprocessed content with encoding and
/// byte-order-mark preservation. A dry run produces the same plan without writing.
/// </summary>
/// <param name="forms">The forms to use.</param>
public partial class TemplateRenderer(ValueFormRegistry forms)
{
    [GeneratedRegex(@"\s+Condition=", RegexOptions.None, 2000)]
    private static partial Regex ConditionAttributeRegex { get; }

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.None, 2000)]
    private static partial Regex GuidRegex { get; }

    /// <summary>
    /// Renders all planned files.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="templateRoot">Absolute path of the template root.</param>
    /// <param name="outputRoot">Absolute path of the output root.</param>
    /// <param name="symbols">Resolved symbol values.</param>
    /// <param name="disabled">Disabled symbol names.</param>
    /// <param name="dryRun">When true, nothing is written and only the plan is returned.</param>
    /// <returns>The files that were created, or would be created in a dry run.</returns>
    public IReadOnlyList<RenderedFile> Render(
        TemplateConfig manifest,
        string templateRoot,
        string outputRoot,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlyList<string> disabled,
        bool dryRun)
    {
        var guids = BuildGuidReplacements(manifest);
        var rendered = new List<RenderedFile>();
        var plan = SourceFileResolver.Plan(manifest, templateRoot, symbols);
        var createdDirectories = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in plan)
        {
            var relativeTarget = ComputeTargetPath(manifest, file, symbols, disabled);
            var absoluteTarget = Path.GetFullPath(Path.Combine(outputRoot, relativeTarget));

            // The placeholder filename: a file named exactly the placeholder creates an empty
            // directory; a file whose name starts with the placeholder strips it (e.g. "_.gitignore").
            var placeholder = manifest.PlaceholderFilename ?? "_";
            var fileName = Path.GetFileName(absoluteTarget);
            if (fileName == placeholder)
            {
                var directory = Path.GetDirectoryName(absoluteTarget)!;
                if (!dryRun)
                {
                    Directory.CreateDirectory(directory);
                }
                createdDirectories.Add(directory);
                continue;
            }

            if (!dryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteTarget)!);
            }

            if (file.CopyOnly)
            {
                if (!dryRun)
                {
                    File.Copy(Path.Combine(file.SourceRoot, file.RelativePath), absoluteTarget, overwrite: true);
                }
                rendered.Add(new RenderedFile(absoluteTarget, WasCopyOnly: true));
                continue;
            }

            var sourcePath = Path.Combine(file.SourceRoot, file.RelativePath);
            var sourceBytes = File.ReadAllBytes(sourcePath);
            var encoding = FileEncoding.Detect(sourceBytes);
            var text = encoding.Encoding.GetString(sourceBytes);
            var (content, _) = TokenAssembly.Build(manifest, symbols, disabled, forms, file.RelativePath);
            var family = FileFamilies.Detect(sourcePath);
            var conditional = TokenAssembly.CustomConditionalFor(
                manifest,
                Path.GetRelativePath(templateRoot, sourcePath).Replace('\\', '/'),
                FileFamilies.ConfigFor(family));

            var needsProcessing =
                (conditional is not null && ConditionalProcessor.ContainsDirectives(text, conditional.Config))
                || content.ContainsAnyToken(text)
                || (family == FileFamily.MSBuild && ConditionAttributeRegex.IsMatch(text))
                || HasIncludeOperations(manifest)
                || guids.Count > 0;
            if (needsProcessing)
            {
                text = ApplyConditionalAndCustomOperations(manifest, text, conditional, symbols, family, sourcePath, templateRoot, manifest.QuotelessChoiceLiterals);
                text = content.Replace(text);
                text = ReplaceGuids(text, guids);
                if (!dryRun)
                {
                    WriteWithEncoding(absoluteTarget, text, encoding);
                }
            }
            else if (!dryRun)
            {
                File.WriteAllBytes(absoluteTarget, sourceBytes);
            }
            rendered.Add(new RenderedFile(absoluteTarget, WasCopyOnly: false));
        }

        return rendered;
    }

    static Dictionary<Guid, Guid> BuildGuidReplacements(TemplateConfig manifest)
    {
        var replacements = new Dictionary<Guid, Guid>();
        foreach (var configured in manifest.Guids)
        {
            replacements[Guid.Parse(configured)] = Guid.NewGuid();
        }
        return replacements;
    }

    static string ReplaceGuids(string text, Dictionary<Guid, Guid> replacements)
    {
        if (replacements.Count == 0)
        {
            return text;
        }

        // GUIDs in the source may use any format and casing; the output preserves both.
        return GuidRegex.Replace(text, match =>
        {
            if (!Guid.TryParseExact(match.Value, "D", out var source) || !replacements.TryGetValue(source, out var replacement))
            {
                return match.Value;
            }
            var isUpper = match.Value.Equals(match.Value.ToUpperInvariant(), StringComparison.Ordinal);
            var replaced = replacement.ToString("D");
            return isUpper ? replaced.ToUpperInvariant() : replaced;
        });
    }

    static string ApplyConditionalAndCustomOperations(
        TemplateConfig manifest,
        string text,
        ConditionalContext? conditional,
        IReadOnlyDictionary<string, string> symbols,
        FileFamily family,
        string sourcePath,
        string templateRoot,
        IReadOnlyCollection<string> knownLiterals)
    {
        if (conditional is not null && ConditionalProcessor.ContainsDirectives(text, conditional.Config))
        {
            text = ConditionalProcessor.Process(text, conditional.Config, conditional.Dialect, symbols, sourcePath, knownLiterals);
        }

        if (family == FileFamily.MSBuild)
        {
            text = ConditionalProcessor.ProcessMsBuildConditions(text, symbols, sourcePath);
        }

        foreach (var operation in CustomOperationsApplyingTo(manifest, sourcePath, templateRoot))
        {
            text = operation.Type switch
            {
                CustomOperationType.Region => StripRegionMarkers(text, operation),
                CustomOperationType.Include => ApplyInclude(text, operation, templateRoot, symbols),
                CustomOperationType.BalancedNesting => StripBalancedNesting(text, operation),
                _ => text
            };
        }
        return text;
    }

    static IEnumerable<CustomOperationConfig> CustomOperationsApplyingTo(
        TemplateConfig manifest, string sourcePath, string templateRoot)
    {
        var relative = Path.GetRelativePath(templateRoot, sourcePath).Replace('\\', '/');
        foreach (var operation in manifest.GlobalCustomOperations)
        {
            if (operation.Type is CustomOperationType.Region or CustomOperationType.Include or CustomOperationType.BalancedNesting)
            {
                yield return operation;
            }
        }
        foreach (var (glob, operations) in manifest.SpecialCustomOperations)
        {
            if (!GlobMatcher.Matches(relative, glob))
            {
                continue;
            }
            foreach (var operation in operations)
            {
                if (operation.Type is CustomOperationType.Region or CustomOperationType.Include or CustomOperationType.BalancedNesting)
                {
                    yield return operation;
                }
            }
        }
    }

    static string StripRegionMarkers(string text, CustomOperationConfig operation)
    {
        var begin = operation.Begin ?? "--#region";
        var end = operation.End ?? "--#endregion";
        var lines = text.Replace("\r\n", "\n").Split('\n');
        return string.Join('\n', lines.Where(line =>
            !line.TrimStart().StartsWith(begin, StringComparison.Ordinal)
            && !line.TrimStart().StartsWith(end, StringComparison.Ordinal)));
    }

    static string ApplyInclude(
        string text,
        CustomOperationConfig operation,
        string templateRoot,
        IReadOnlyDictionary<string, string> symbols)
    {
        if (operation.Token is null || operation.IncludePath is null)
        {
            return text;
        }
        var includePath = Path.Combine(templateRoot, operation.IncludePath);
        if (!File.Exists(includePath))
        {
            throw new InvalidTemplateManifest($"include operation references missing file '{operation.IncludePath}'.");
        }
        var included = Decode(File.ReadAllBytes(includePath));
        var family = FileFamilies.Detect(includePath);
        var conditional = FileFamilies.ConfigFor(family);
        if (ConditionalProcessor.ContainsDirectives(included, conditional))
        {
            included = ConditionalProcessor.Process(included, conditional, ExpressionDialect.Cpp2, symbols, includePath);
        }
        return text.Replace(operation.Token, included, StringComparison.Ordinal);
    }

    static string StripBalancedNesting(string text, CustomOperationConfig operation)
    {
        var begin = operation.Begin ?? "--#begin";
        var end = operation.End ?? "--#end";
        var lines = text.Replace("\r\n", "\n").Split('\n');
        return string.Join('\n', lines.Where(line =>
            !line.TrimStart().StartsWith(begin, StringComparison.Ordinal)
            && !line.TrimStart().StartsWith(end, StringComparison.Ordinal)));
    }

    static bool HasIncludeOperations(TemplateConfig manifest) =>
        manifest.GlobalCustomOperations.Concat(manifest.SpecialCustomOperations.Values.SelectMany(operations => operations))
            .Any(operation => operation.Type is CustomOperationType.Include or CustomOperationType.Region or CustomOperationType.BalancedNesting);

    static string ComputeTargetPath(
        TemplateConfig manifest,
        PlannedFile file,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlyList<string> disabled)
    {
        // Renames first — pattern-based, evaluated against the source-relative path — then
        // token replacement on the resulting path (sourceName forms and fileRename tokens).
        var path = file.RelativePath;
        foreach (var rename in file.Renames)
        {
            path = Regex.Replace(path, rename.Pattern, rename.Replacement, RegexOptions.None, TimeSpan.FromSeconds(2));
        }

        var (_, pathTokens) = TokenAssembly.Build(manifest, symbols, disabled, ValueFormRegistry.Empty, file.RelativePath);
        path = pathTokens.ReplacePath(path);

        return string.IsNullOrEmpty(file.TargetRoot) || string.Equals(file.TargetRoot, "./", StringComparison.Ordinal) || string.Equals(file.TargetRoot, ".", StringComparison.Ordinal)
            ? path
            : $"{file.TargetRoot.TrimEnd('/')}/{path}";
    }

    static string Decode(byte[] bytes) => FileEncoding.Detect(bytes).Encoding.GetString(bytes);

    static void WriteWithEncoding(string path, string text, FileEncoding encoding)
    {
        var preamble = encoding.HasBom ? encoding.Encoding.GetPreamble() : [];
        var bytes = preamble.Concat(encoding.Encoding.GetBytes(text)).ToArray();
        File.WriteAllBytes(path, bytes);
    }
}
