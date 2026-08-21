// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Loads application source once and delegates generation to the selected framework provider.
/// </summary>
public sealed class ProviderScreenplayGeneration : IScreenplayGeneration
{
    /// <inheritdoc/>
    public async Task<GeneratedScreenplay> Generate(
        string targetPath,
        ScreenplayGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var requested = options.Provider.ToLowerInvariant();
        if (!ScreenplayProviders.IsKnown(requested))
        {
            return InvalidProvider(requested, targetPath);
        }

        var loaded = await ScreenplayCompilationLoader.Load(
            targetPath,
            includeAllProjects: !string.Equals(requested, ScreenplayProviders.Arc, StringComparison.Ordinal),
            cancellationToken);
        var provider = string.Equals(requested, ScreenplayProviders.Auto, StringComparison.Ordinal)
            ? Detect(loaded)
            : requested;

        return provider switch
        {
            ScreenplayProviders.Arc => ArcScreenplayGeneration.GenerateFrom(NarrowToArc(loaded), targetPath, options),
            ScreenplayProviders.Marten or ScreenplayProviders.CritterStack =>
                CritterStackScreenplayGeneration.GenerateFrom(loaded, targetPath, options),
            _ => InvalidProvider(provider, targetPath)
        };
    }

    static string Detect(LoadedCompilation loaded) => loaded.Compilations.Any(IsCritterStack)
        ? ScreenplayProviders.CritterStack
        : ScreenplayProviders.Arc;

    static bool IsCritterStack(Microsoft.CodeAnalysis.Compilation compilation) =>
        compilation.GetTypeByMetadataName("Marten.StoreOptions") is not null ||
        compilation.GetTypeByMetadataName("Marten.IDocumentStore") is not null ||
        compilation.GetTypeByMetadataName("Wolverine.WolverineOptions") is not null;

    static LoadedCompilation NarrowToArc(LoadedCompilation loaded)
    {
        var selected = loaded.Compilations
            .Select((compilation, index) => new { Compilation = compilation, Name = loaded.ProjectNames[index] })
            .Where(_ => ScreenplayProjectSelection.CanDeclareAnArtifact(_.Compilation))
            .ToArray();
        return selected.Length == loaded.Compilations.Count
            ? loaded
            : new LoadedCompilation(
                [.. selected.Select(_ => _.Compilation)],
                [.. selected.Select(_ => _.Name)],
                loaded.Diagnostics);
    }

    static GeneratedScreenplay InvalidProvider(string provider, string targetPath) => new(
        string.Empty,
        [
            new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Error,
                ScreenplayDiagnosticCodes.InvalidProvider,
                $"Unknown Screenplay provider '{provider}'. Use auto, arc, marten, or critter-stack",
                targetPath)
        ]);
}
