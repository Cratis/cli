// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Provides the allowlisted source-framework providers bundled with this CLI build.
/// </summary>
static class ScreenplaySourceProviders
{
    public static readonly IReadOnlyList<IScreenplaySourceProvider> Default =
    [
        new ArcSourceProvider(),
        new MartenSourceProvider(),
        new CritterStackSourceProvider()
    ];
}

static class ProviderEvidence
{
    public static bool HasMarten(Microsoft.CodeAnalysis.Compilation compilation) =>
        compilation.GetTypeByMetadataName("Marten.StoreOptions") is not null ||
        compilation.GetTypeByMetadataName("Marten.IDocumentStore") is not null;

    public static bool HasWolverine(Microsoft.CodeAnalysis.Compilation compilation) =>
        compilation.GetTypeByMetadataName("Wolverine.WolverineOptions") is not null;
}

sealed class ArcSourceProvider : IScreenplaySourceProvider
{
    public string Name => ScreenplayProviders.Arc;
    public IReadOnlyList<string> Supersedes => [];
    public bool RequiresSingleHost => false;
    public bool Matches(LoadedCompilation loaded) => loaded.Compilations.Any(ScreenplayProjectSelection.CanDeclareAnArtifact);
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        ArcScreenplayGeneration.GenerateFrom(Narrow(loaded), targetPath, options);

    static LoadedCompilation Narrow(LoadedCompilation loaded)
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
}

sealed class MartenSourceProvider : IScreenplaySourceProvider
{
    public string Name => ScreenplayProviders.Marten;
    public IReadOnlyList<string> Supersedes => [];
    public bool RequiresSingleHost => true;
    public bool Matches(LoadedCompilation loaded) => loaded.Compilations.Any(ProviderEvidence.HasMarten);
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        CritterStackScreenplayGeneration.GenerateFrom(loaded, targetPath, options);
}

sealed class CritterStackSourceProvider : IScreenplaySourceProvider
{
    public string Name => ScreenplayProviders.CritterStack;
    public IReadOnlyList<string> Supersedes => [ScreenplayProviders.Marten];
    public bool RequiresSingleHost => true;
    public bool Matches(LoadedCompilation loaded) => loaded.Compilations.Any(_ =>
        ProviderEvidence.HasMarten(_) && ProviderEvidence.HasWolverine(_));
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        CritterStackScreenplayGeneration.GenerateFrom(loaded, targetPath, options);
}
