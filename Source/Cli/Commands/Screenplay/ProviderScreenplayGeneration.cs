// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Loads application source once, discovers bundled provider capabilities, and delegates generation.
/// </summary>
public sealed class ProviderScreenplayGeneration : IScreenplayGeneration
{
    readonly IReadOnlyList<IScreenplaySourceProvider> _providers;

    /// <summary>
    /// Initializes generation with every allowlisted provider bundled into this CLI build.
    /// </summary>
    public ProviderScreenplayGeneration()
        : this(ScreenplaySourceProviders.Default)
    {
    }

    internal ProviderScreenplayGeneration(IReadOnlyList<IScreenplaySourceProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc/>
    public async Task<GeneratedScreenplay> Generate(
        string targetPath,
        ScreenplayGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var requested = options.Provider.ToLowerInvariant();
        var explicitProvider = string.Equals(requested, ScreenplayProviders.Auto, StringComparison.Ordinal)
            ? null
            : _providers.FirstOrDefault(_ => string.Equals(_.Name, requested, StringComparison.Ordinal));
        if (requested != ScreenplayProviders.Auto && explicitProvider is null)
        {
            return InvalidProvider(requested, targetPath);
        }

        var loaded = await ScreenplayCompilationLoader.Load(targetPath, includeAllProjects: true, cancellationToken);
        if (loaded.Compilations.Count == 0)
        {
            return new GeneratedScreenplay(string.Empty, loaded.Diagnostics)
            {
                Projects = loaded.ProjectNames,
                Provenance = explicitProvider is null
                    ? null
                    : new ScreenplayGenerationProvenance(explicitProvider.Name, explicitProvider.Version, loaded.ProjectProvenance, null)
            };
        }

        var selection = explicitProvider is null ? Discover(loaded, targetPath) : new ProviderSelection(explicitProvider, null);
        if (selection.Error is not null)
        {
            return selection.Error with
            {
                Diagnostics = [.. loaded.Diagnostics, .. selection.Error.Diagnostics],
                Projects = loaded.ProjectNames
            };
        }

        var provider = selection.Provider!;
        var selected = provider.SelectFrom(loaded);
        var compatibility = ScreenplayCompatibility.Evaluate(provider, selected);
        if (compatibility.BlockingDiagnostic is not null)
        {
            return new GeneratedScreenplay(
                string.Empty,
                [.. loaded.Diagnostics, compatibility.BlockingDiagnostic])
            {
                Projects = selected.ProjectNames,
                Provenance = compatibility.Provenance
            };
        }

        var generated = AmbiguousHosts(selected, targetPath, provider) ?? provider.GenerateFrom(selected, targetPath, options);
        return generated with
        {
            Provenance = compatibility.Complete(generated.Diagnostics)
        };
    }

    internal ProviderSelection Discover(LoadedCompilation loaded, string targetPath)
    {
        var matches = _providers.Where(_ => _.Matches(loaded)).ToArray();
        var superseded = matches.SelectMany(_ => _.Supersedes).ToHashSet(StringComparer.Ordinal);
        matches = [.. matches.Where(_ => !superseded.Contains(_.Name))];

        return matches.Length switch
        {
            1 => new(matches[0], null),
            0 => new(null, ProviderError(
                ScreenplayDiagnosticCodes.NoMatchingProvider,
                $"No bundled Screenplay provider recognizes the loaded source. Available providers: {ProviderNames()}",
                targetPath)),
            _ => new(null, ProviderError(
                ScreenplayDiagnosticCodes.AmbiguousProviders,
                $"Several Screenplay providers recognize the loaded source: {string.Join(", ", matches.Select(_ => _.Name).Order(StringComparer.Ordinal))}. Select one with --provider",
                targetPath))
        };
    }

    internal GeneratedScreenplay? AmbiguousHosts(
        LoadedCompilation loaded,
        string targetPath,
        IScreenplaySourceProvider provider)
    {
        if (!provider.RequiresSingleHost || !ScreenplayTargetResolver.IsSolution(targetPath))
        {
            return null;
        }

        var hosts = loaded.Compilations
            .Select((compilation, index) => new { EntryPoint = compilation.GetEntryPoint(CancellationToken.None), Name = loaded.ProjectNames[index] })
            .Where(_ => _.EntryPoint is not null)
            .Select(_ => _.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return hosts.Length <= 1
            ? null
            : ProviderError(
                ScreenplayDiagnosticCodes.AmbiguousApplicationHosts,
                $"Solution contains several deployable application hosts: {string.Join(", ", hosts)}. Target one .csproj explicitly",
                targetPath);
    }

    string ProviderNames() => string.Join(", ", _providers.Select(_ => _.Name).Order(StringComparer.Ordinal));

    GeneratedScreenplay InvalidProvider(string provider, string targetPath) => ProviderError(
        ScreenplayDiagnosticCodes.InvalidProvider,
        $"Unknown Screenplay provider '{provider}'. Available providers: {ProviderNames()}",
        targetPath);

    GeneratedScreenplay ProviderError(string code, string message, string targetPath) => new(
        string.Empty,
        [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, code, message, targetPath)]);
}

internal sealed record ProviderSelection(
    IScreenplaySourceProvider? Provider,
    GeneratedScreenplay? Error);
