// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

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

static class ProviderAssemblyVersion
{
    public static string Of(Type providerType, string packageId)
    {
        var packageVersion = typeof(ProviderAssemblyVersion).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(metadata => string.Equals(metadata.Key, $"{packageId}.PackageVersion", StringComparison.Ordinal))
            ?.Value;
        if (!string.IsNullOrWhiteSpace(packageVersion))
        {
            return packageVersion;
        }

        var assembly = providerType.Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(informational)
            ? assembly.GetName().Version?.ToString() ?? "unknown"
            : informational.Split('+')[0];
    }
}

sealed class ArcSourceProvider : IScreenplaySourceProvider
{
    public string Name => ScreenplayProviders.Arc;
    public string Version => ProviderAssemblyVersion.Of(typeof(Cratis.Arc.Screenplay.ScreenplayGenerator), "Cratis.Arc.Screenplay");
    public IReadOnlyList<string> Supersedes => [];
    public bool RequiresSingleHost => false;
    public bool Matches(LoadedCompilation loaded) => loaded.Compilations.Any(ScreenplayProjectSelection.CanDeclareAnArtifact);
    public LoadedCompilation SelectFrom(LoadedCompilation loaded) => Narrow(loaded);
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        ArcScreenplayGeneration.GenerateFrom(Narrow(loaded), targetPath, options);

    static LoadedCompilation Narrow(LoadedCompilation loaded)
    {
        if (loaded.ProjectSourceAlignmentFailureFor(null) is { } alignmentFailure)
        {
            return new LoadedCompilation([], [], [.. loaded.Diagnostics, alignmentFailure]);
        }

        var selectedIndices = loaded.Compilations
            .Select((compilation, index) => new { Compilation = compilation, Index = index })
            .Where(_ => ScreenplayProjectSelection.CanDeclareAnArtifact(_.Compilation))
            .Select(_ => _.Index)
            .ToArray();
        return selectedIndices.Length == loaded.Compilations.Count
            ? loaded
            : new LoadedCompilation(
                [.. selectedIndices.Select(index => loaded.Compilations[index])],
                [.. selectedIndices.Select(index => loaded.ProjectNames[index])],
                loaded.Diagnostics)
            {
                AuthoredSyntaxTrees = SelectedFrom(loaded.AuthoredSyntaxTrees, selectedIndices, loaded.Compilations.Count),
                ProjectProvenance = SelectedFrom(loaded.ProjectProvenance, selectedIndices, loaded.Compilations.Count),
                ProjectSources = SelectedFrom(loaded.ProjectSources, selectedIndices, loaded.Compilations.Count)
            };
    }

    static IReadOnlyList<T> SelectedFrom<T>(IReadOnlyList<T> values, IEnumerable<int> selectedIndices, int compilationCount) =>
        values.Count == compilationCount ? [.. selectedIndices.Select(index => values[index])] : [];
}

sealed class MartenSourceProvider : IScreenplaySourceProvider
{
    public string Name => ScreenplayProviders.Marten;
    public string Version => ProviderAssemblyVersion.Of(typeof(Cratis.CritterStack.Screenplay.CritterStackScreenplayGenerator), "Cratis.CritterStack.Screenplay");
    public IReadOnlyList<string> Supersedes => [];
    public bool RequiresSingleHost => true;
    public bool Matches(LoadedCompilation loaded) => loaded.Compilations.Any(ProviderEvidence.HasMarten);
    public LoadedCompilation SelectFrom(LoadedCompilation loaded) => loaded;
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        CritterStackScreenplayGeneration.GenerateFrom(loaded, targetPath, options);
}

sealed class CritterStackSourceProvider : IScreenplaySourceProvider
{
    public string Name => ScreenplayProviders.CritterStack;
    public string Version => ProviderAssemblyVersion.Of(typeof(Cratis.CritterStack.Screenplay.CritterStackScreenplayGenerator), "Cratis.CritterStack.Screenplay");
    public IReadOnlyList<string> Supersedes => [ScreenplayProviders.Marten];
    public bool RequiresSingleHost => true;
    public bool Matches(LoadedCompilation loaded) =>
        loaded.Compilations.Any(ProviderEvidence.HasMarten) &&
        loaded.Compilations.Any(ProviderEvidence.HasWolverine);
    public LoadedCompilation SelectFrom(LoadedCompilation loaded) => loaded;
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        CritterStackScreenplayGeneration.GenerateFrom(loaded, targetPath, options);
}
