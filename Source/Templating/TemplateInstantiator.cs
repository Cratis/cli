// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;
using Cratis.Templating.Expressions;
using Cratis.Templating.Orchestration;
using Cratis.Templating.Symbols;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating;

/// <summary>
/// The inputs to a template instantiation.
/// </summary>
/// <param name="Name">The name for the instantiation; null uses the manifest default or output directory name.</param>
/// <param name="OutputPath">The output directory; null uses the current directory.</param>
/// <param name="ParameterValues">User-provided parameter values keyed by symbol name.</param>
/// <param name="Baseline">The baseline name, when selected.</param>
/// <param name="DryRun">Whether to only report what would be created.</param>
/// <param name="Force">Whether to allow writing into a non-empty output directory.</param>
public record InstantiationInputs(
    string? Name,
    string? OutputPath,
    IReadOnlyDictionary<string, string> ParameterValues,
    string? Baseline = null,
    bool DryRun = false,
    bool Force = false);

/// <summary>
/// The result of an instantiation.
/// </summary>
/// <param name="Name">The effective instantiation name.</param>
/// <param name="OutputRoot">The absolute output root.</param>
/// <param name="CreatedFiles">The files created, or that would be created in a dry run.</param>
/// <param name="PrimaryOutputs">The resolved primary output paths.</param>
/// <param name="SymbolValues">The resolved symbol values.</param>
/// <param name="DisabledSymbols">Symbols disabled by condition.</param>
public record InstantiationResult(
    string Name,
    string OutputRoot,
    IReadOnlyList<RenderedFile> CreatedFiles,
    IReadOnlyList<string> PrimaryOutputs,
    IReadOnlyDictionary<string, string> SymbolValues,
    IReadOnlyList<string> DisabledSymbols);

/// <summary>
/// Instantiates templates: resolves the name and output directory with dotnet-new semantics
/// (<c language="csharp">preferNameDirectory</c>, <c language="csharp">preferDefaultName</c>, <c language="csharp">defaultName</c>), applies baselines, resolves
/// symbols, renders, and collects primary outputs.
/// </summary>
public class TemplateInstantiator
{
    readonly ValueFormRegistry _forms;
    readonly TemplateRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateInstantiator"/> class.
    /// </summary>
    /// <param name="forms">The value form registry.</param>
    public TemplateInstantiator(ValueFormRegistry? forms = null)
    {
        _forms = forms ?? ValueFormRegistry.Empty;
        _renderer = new TemplateRenderer(_forms);
    }

    /// <summary>
    /// Instantiates a template from an unpacked template directory.
    /// </summary>
    /// <param name="manifest">The parsed manifest.</param>
    /// <param name="templateRoot">The template root directory (the one containing .template.config).</param>
    /// <param name="inputs">The instantiation inputs.</param>
    /// <returns>The instantiation result.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public InstantiationResult Instantiate(TemplateConfig manifest, string templateRoot, InstantiationInputs inputs)
    {
        var name = ResolveName(manifest, inputs);
        var outputRoot = ResolveOutputRoot(manifest, inputs, name);
        outputRoot = Path.GetFullPath(outputRoot);

        if (!inputs.DryRun)
        {
            if (!inputs.Force && Directory.Exists(outputRoot) && Directory.EnumerateFileSystemEntries(outputRoot).Any())
            {
                throw new InvalidTemplateManifest(
                    $"output directory '{outputRoot}' is not empty. Use --force to allow writing into it.");
            }
            Directory.CreateDirectory(outputRoot);
        }

        var parameters = ApplyBaseline(manifest, inputs);
        var resolver = new SymbolResolver(_forms);
        var hostData = new Dictionary<string, string> { ["name"] = name };
        var symbols = resolver.Resolve(manifest, parameters, name, hostData);
        ValidateRequiredParameters(manifest, symbols, parameters);

        var rendered = _renderer.Render(manifest, templateRoot, outputRoot, symbols.Values, symbols.DisabledSymbols, inputs.DryRun);
        var primaryOutputs = ResolvePrimaryOutputs(manifest, outputRoot, symbols.Values, symbols.DisabledSymbols, name);

        return new InstantiationResult(name, outputRoot, rendered, primaryOutputs, symbols.Values, symbols.DisabledSymbols);
    }

    static string ResolveName(TemplateConfig manifest, InstantiationInputs inputs)
    {
        if (manifest.PreferDefaultName && manifest.DefaultName is not null && inputs.Name is not null && inputs.Name != manifest.DefaultName)
        {
            throw new InvalidTemplateManifest(
                $"template '{manifest.ShortName}' prefers its default name '{manifest.DefaultName}' — the name cannot be changed.");
        }
        return inputs.Name
            ?? manifest.DefaultName
            ?? (inputs.OutputPath is not null ? Path.GetFileName(Path.GetFullPath(inputs.OutputPath)) : new DirectoryInfo(Environment.CurrentDirectory).Name);
    }

    static string ResolveOutputRoot(TemplateConfig manifest, InstantiationInputs inputs, string name)
    {
        return inputs.OutputPath ?? (manifest.PreferNameDirectory
            ? Path.Combine(Environment.CurrentDirectory, name)
            : Environment.CurrentDirectory);
    }

    static Dictionary<string, string> ApplyBaseline(TemplateConfig manifest, InstantiationInputs inputs)
    {
        var parameters = new Dictionary<string, string>(inputs.ParameterValues, StringComparer.Ordinal);
        if (inputs.Baseline is null)
        {
            return parameters;
        }

        if (!manifest.Baselines.TryGetValue(inputs.Baseline, out var baseline))
        {
            throw new InvalidTemplateManifest(
                $"baseline '{inputs.Baseline}' is not defined. Known baselines: {string.Join(", ", manifest.Baselines.Keys.Order())}.");
        }

        foreach (var (symbol, value) in baseline.Symbols)
        {
            // User-provided values win over baseline defaults.
            if (!parameters.ContainsKey(symbol))
            {
                parameters[symbol] = value;
            }
        }
        return parameters;
    }

    static void ValidateRequiredParameters(
        TemplateConfig manifest,
        ResolvedSymbols symbols,
        Dictionary<string, string> provided)
    {
        foreach (var (name, symbol) in manifest.Symbols)
        {
            if (symbol.Type != SymbolType.Parameter || symbols.DisabledSymbols.Contains(name))
            {
                continue;
            }

            var required = symbol.IsRequired is not null && ExpressionEvaluator.EvaluateBoolean(symbol.IsRequired, ExpressionDialect.Cpp2, symbols.Values);
            if (required && !provided.ContainsKey(name) && symbol.DefaultValue is null)
            {
                throw new SymbolResolutionError(
                    $"parameter '{name}' is required by template '{manifest.ShortName}' but no value was provided.");
            }
        }
    }

    static List<string> ResolvePrimaryOutputs(
        TemplateConfig manifest,
        string outputRoot,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlyList<string> disabled,
        string name)
    {
        var outputs = new List<string>();
        foreach (var output in manifest.PrimaryOutputs)
        {
            if (output.Condition is not null
                && !ExpressionEvaluator.EvaluateBoolean(output.Condition, ExpressionDialect.Cpp2, symbols))
            {
                continue;
            }

            var (content, paths) = TokenAssembly.Build(manifest, symbols, disabled, ValueFormRegistry.Empty, output.Path);
            _ = content;
            var resolved = paths.ReplacePath(output.Path);
            if (manifest.SourceName is not null)
            {
                foreach (var form in ValueFormRegistry.SourceNameDefaultForms)
                {
                    resolved = resolved.Replace(
                        ValueFormRegistry.Empty.Apply(form, manifest.SourceName),
                        ValueFormRegistry.Empty.Apply(form, name),
                        StringComparison.Ordinal);
                }
            }
            outputs.Add(Path.GetFullPath(Path.Combine(outputRoot, resolved)));
        }
        return outputs;
    }
}
