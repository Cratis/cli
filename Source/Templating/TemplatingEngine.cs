// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating;

/// <summary>
/// The evaluation of one constraint: satisfied, unsatisfied (the template must not be used here) or
/// unevaluatable (needs a toolchain we do not have — reported, never treated as satisfied or failed).
/// </summary>
public enum ConstraintEvaluation
{
    /// <summary>The constraint holds.</summary>
    Satisfied,

    /// <summary>The constraint does not hold — the template is not usable in this environment.</summary>
    Unsatisfied,

    /// <summary>The constraint cannot be evaluated without a toolchain; distinct from unsatisfied.</summary>
    Unevaluatable
}

/// <summary>
/// Constraint checking per the documented constraint types: <c language="csharp">os</c>, <c language="csharp">host</c>, <c language="csharp">sdk-version</c>,
/// <c language="csharp">workload</c> and <c language="csharp">project-capability</c>. The last three need a toolchain; on a machine without
/// dotnet they report as unevaluatable rather than unsatisfied.
/// </summary>
public static class TemplateConstraints
{
    /// <summary>
    /// Evaluates all constraints of a manifest.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <returns>Evaluations keyed by constraint name.</returns>
    public static IReadOnlyDictionary<string, ConstraintEvaluation> Evaluate(TemplateConfig manifest) =>
        manifest.Constraints.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Type switch
            {
                "os" => EvaluateOs(pair.Value),
                "host" => EvaluateHost(pair.Value),
                _ => ConstraintEvaluation.Unevaluatable
            });

    static ConstraintEvaluation EvaluateOs(ConstraintConfig constraint)
    {
        string current;
        if (OperatingSystem.IsWindows())
        {
            current = "Windows";
        }
        else if (OperatingSystem.IsMacOS())
        {
            current = "OSX";
        }
        else
        {
            current = "Linux";
        }
        return constraint.Allowed.Any(allowed => allowed.Equals(current, StringComparison.OrdinalIgnoreCase))
            ? ConstraintEvaluation.Satisfied
            : ConstraintEvaluation.Unsatisfied;
    }

    static ConstraintEvaluation EvaluateHost(ConstraintConfig constraint)
    {
        // The documented host constraint targets host names such as "dotnetcli" with optional version
        // ranges. This engine's host identity is "cratiscli"; anything else is unsatisfied.
        const string hostName = "cratiscli";
        return constraint.Allowed.Any(allowed => allowed.Equals(hostName, StringComparison.OrdinalIgnoreCase))
            ? ConstraintEvaluation.Satisfied
            : ConstraintEvaluation.Unsatisfied;
    }
}

/// <summary>
/// The facade over the whole engine: package acquisition, template discovery, instantiation and post
/// actions. The CLI talks to the engine through this type.
/// </summary>
/// <param name="storeRoot">The storeRoot to use.</param>
public class TemplatingEngine(string storeRoot)
{
    /// <summary>
    /// Gets the package store this engine acquires packages into.
    /// </summary>
    public TemplatePackageStore Store { get; } = new(storeRoot);

    /// <summary>
    /// Acquires a template package from the configured feeds and discovers its templates.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="versionConstraint">Exact version, version prefix, or * for latest.</param>
    /// <param name="workingDirectory">Directory whose NuGet configuration applies.</param>
    /// <returns>The discovered templates.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public async Task<IReadOnlyList<DiscoveredTemplate>> Acquire(string packageId, string versionConstraint, string workingDirectory)
    {
        foreach (var feed in NuGetConfig.DiscoverFeeds(workingDirectory))
        {
            try
            {
                var version = await Store.ResolveVersion(feed, packageId, versionConstraint);
                var packageRoot = await Store.Acquire(feed, packageId, version);
                return TemplatePackageStore.DiscoverTemplates(packageRoot);
            }
            catch (TemplatePackageAcquisitionError)
            {
                // Try the next feed.
            }
        }
        throw new TemplatePackageAcquisitionError(
            $"could not acquire template package '{packageId}' {versionConstraint} from any configured feed.");
    }

    /// <summary>
    /// Discovers templates from a local package folder or .nupkg, so unpublished packages can be exercised.
    /// </summary>
    /// <param name="path">The folder or .nupkg path.</param>
    /// <returns>The discovered templates.</returns>
    public IReadOnlyList<DiscoveredTemplate> DiscoverLocal(string path)
    {
        var root = Store.InstallLocal(path);
        return TemplatePackageStore.DiscoverTemplates(root);
    }

    /// <summary>
    /// Instantiates a template and runs its post actions.
    /// </summary>
    /// <param name="template">The discovered template.</param>
    /// <param name="inputs">The instantiation inputs.</param>
    /// <param name="scriptPolicy">The policy for script post actions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The instantiation result and post action results.</returns>
    public async Task<(InstantiationResult Creation, IReadOnlyList<PostActionResult> Actions)> Instantiate(
        DiscoveredTemplate template,
        InstantiationInputs inputs,
        ScriptPolicy scriptPolicy,
        CancellationToken cancellationToken = default)
    {
        var forms = new ValueFormRegistry(template.Manifest.Forms);
        var instantiator = new TemplateInstantiator(forms);
        var creation = instantiator.Instantiate(template.Manifest, template.Directory, inputs);

        // A dry run writes nothing, so post actions have nothing to operate on — they are
        // reported as skipped rather than attempted.
        if (inputs.DryRun)
        {
            var skipped = template.Manifest.PostActions
                .Select(action => new PostActionResult(action, PostActionOutcome.Skipped, "dry run — nothing was written", string.Empty))
                .ToArray();
            return (creation, skipped);
        }

        var runner = new PostActionRunner(Store, scriptPolicy);
        var actions = await runner.Run(template.Manifest, creation, creation.SymbolValues, cancellationToken);
        return (creation, actions);
    }
}
