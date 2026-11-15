// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.PostActions;

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// Creates an extension package by instantiating the template that matches the request.
/// </summary>
/// <param name="engine">The templating engine, over the CLI's template store.</param>
public class ExtensionScaffolder(TemplatingEngine engine)
{
    /// <summary>
    /// Creates a package.
    /// </summary>
    /// <param name="request">The request, already validated by <see cref="ExtensionScaffoldValidator"/>.</param>
    /// <param name="target">Where the templates come from and where the package goes.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>What was created, or why nothing was.</returns>
    public async Task<ExtensionScaffoldResult> Create(ExtensionScaffoldRequest request, ExtensionScaffoldTarget target, CancellationToken cancellationToken)
    {
        var shortName = ExtensionTemplateInterface.ShortNameOf(request.Kind);
        var templates = target.TemplatePath is not null
            ? engine.DiscoverLocal(target.TemplatePath)
            : await engine.Acquire(target.TemplatePackage ?? ExtensionTemplateInterface.PackageId, target.TemplateVersion ?? "*", Environment.CurrentDirectory);

        var template = templates.FirstOrDefault(candidate => candidate.Manifest.ShortName.Equals(shortName, StringComparison.OrdinalIgnoreCase));
        if (template is null)
        {
            var found = templates.Count == 0 ? "none" : string.Join(", ", templates.Select(candidate => candidate.Manifest.ShortName));
            return ExtensionScaffoldResult.Failed($"The template '{shortName}' was not found. Templates available: {found}.");
        }

        var parameters = ExtensionTemplateInterface.ParametersOf(request);
        var problems = Mismatches(template.Manifest.Symbols, parameters);
        if (problems.Count > 0)
        {
            return ExtensionScaffoldResult.Failed($"The template '{shortName}' does not support this request: {string.Join(' ', problems)}");
        }

        var inputs = new InstantiationInputs(request.Name, target.Output, parameters, null, target.DryRun, target.Force);
        var (creation, actions) = await engine.Instantiate(template, inputs, ScriptPolicy.Deny, cancellationToken);
        return new ExtensionScaffoldResult(creation, actions, null);
    }

    static List<string> Mismatches(IReadOnlyDictionary<string, Cratis.Templating.Configuration.SymbolConfig> symbols, IReadOnlyDictionary<string, string> parameters)
    {
        var problems = new List<string>();
        foreach (var (name, value) in parameters)
        {
            if (!symbols.TryGetValue(name, out var symbol))
            {
                problems.Add($"It declares no symbol '{name}'.");
            }
            else if (symbol.Choices.Count > 0 && !symbol.Choices.Any(choice => choice.Choice.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add($"Symbol '{name}' does not offer '{value}' (it offers {string.Join(", ", symbol.Choices.Select(choice => choice.Choice))}).");
            }
        }

        return problems;
    }
}
