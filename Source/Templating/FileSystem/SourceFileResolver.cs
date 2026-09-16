// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;
using Cratis.Templating.Expressions;

namespace Cratis.Templating.FileSystem;

/// <summary>
/// One planned file: its location in the template, whether it is copy-only, and the effective rename
/// rules that apply to it.
/// </summary>
/// <param name="RelativePath">Path relative to the source root, using forward slashes.</param>
/// <param name="SourceRoot">Absolute path of the source root this file belongs to.</param>
/// <param name="TargetRoot">Relative target root inside the output.</param>
/// <param name="CopyOnly">Whether the file is copied byte-exact without processing.</param>
/// <param name="Renames">Rename rules that survived their conditions.</param>
public record PlannedFile(string RelativePath, string SourceRoot, string TargetRoot, bool CopyOnly, IReadOnlyList<RenameConfig> Renames);

/// <summary>
/// Enumerates the files each <c language="csharp">sources</c> entry contributes, applying include, exclude, copyOnly and
/// rename rules from the entry and its modifiers. The <c language="csharp">.template.config</c> directory is always excluded.
/// </summary>
public static class SourceFileResolver
{
    /// <summary>
    /// Plans all files for a manifest.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="templateRoot">Absolute path of the template root.</param>
    /// <param name="scope">Resolved symbol values for modifier conditions.</param>
    /// <returns>The planned files.</returns>
    public static IReadOnlyList<PlannedFile> Plan(
        TemplateConfig manifest,
        string templateRoot,
        IReadOnlyDictionary<string, string> scope)
    {
        var planned = new List<PlannedFile>();
        foreach (var source in manifest.Sources.Count > 0 ? manifest.Sources : [new SourceConfig()])
        {
            if (source.Condition is not null
                && !ExpressionEvaluator.EvaluateBoolean(source.Condition, ExpressionDialect.Cpp2, scope))
            {
                continue;
            }

            var (include, exclude, copyOnly, renames) = EffectiveRules(source, scope);
            var sourceRoot = Path.GetFullPath(Path.Combine(templateRoot, source.Source));
            if (!Directory.Exists(sourceRoot))
            {
                continue;
            }

            foreach (var file in EnumerateFiles(sourceRoot))
            {
                var relative = Path.GetRelativePath(sourceRoot, file).Replace('\\', '/');
                if (!Matches(relative, include, exclude))
                {
                    continue;
                }
                planned.Add(new PlannedFile(
                    relative,
                    sourceRoot,
                    source.Target,
                    copyOnly.Exists(glob => GlobMatcher.Matches(relative, glob)),
                    renames));
            }
        }
        return planned;
    }

    static (List<string> Include, List<string> Exclude, List<string> CopyOnly, List<RenameConfig> Renames) EffectiveRules(
        SourceConfig source,
        IReadOnlyDictionary<string, string> scope)
    {
        var include = new List<string>(source.Include);
        var exclude = new List<string>(source.Exclude);
        var copyOnly = new List<string>(source.CopyOnly);
        var renames = new List<RenameConfig>(source.Rename);

        foreach (var modifier in source.Modifiers)
        {
            if (modifier.Condition is not null
                && !ExpressionEvaluator.EvaluateBoolean(modifier.Condition, ExpressionDialect.Cpp2, scope))
            {
                continue;
            }
            if (modifier.Include.Count > 0)
            {
                // A modifier with includes narrows the previous include set; without a prior
                // include set it adds to the everything-default.
                include = include.Count > 0
                    ? [.. include.Where(existing => modifier.Include.Any(narrow => GlobMatcher.Matches(existing, narrow) || GlobMatcher.Matches(narrow, existing)))]
                    : [.. modifier.Include];
            }
            exclude.AddRange(modifier.Exclude);
            copyOnly.AddRange(modifier.CopyOnly);
            renames.AddRange(modifier.Rename);
        }

        exclude.Add("**/.template.config/**");
        return (include, exclude, copyOnly, renames);
    }

    static bool Matches(string relative, List<string> include, List<string> exclude)
    {
        if (exclude.Exists(glob => GlobMatcher.Matches(relative, glob)))
        {
            return false;
        }
        return include.Count == 0 || include.Exists(glob => GlobMatcher.Matches(relative, glob));
    }

    static IEnumerable<string> EnumerateFiles(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
}
