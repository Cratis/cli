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
        var selected = loaded.Compilations
            .Select((compilation, index) => new
            {
                Compilation = compilation,
                Name = loaded.ProjectNames[index],
                AuthoredSyntaxTrees = loaded.AuthoredSyntaxTrees.Count > index ? loaded.AuthoredSyntaxTrees[index] : null,
                Provenance = loaded.ProjectProvenance.Count > index ? loaded.ProjectProvenance[index] : null
            })
            .Where(_ => ScreenplayProjectSelection.CanDeclareAnArtifact(_.Compilation))
            .ToArray();
        return selected.Length == loaded.Compilations.Count
            ? loaded
            : new LoadedCompilation(
                [.. selected.Select(_ => _.Compilation)],
                [.. selected.Select(_ => _.Name)],
                loaded.Diagnostics)
            {
                AuthoredSyntaxTrees = [.. selected.Where(_ => _.AuthoredSyntaxTrees is not null).Select(_ => _.AuthoredSyntaxTrees!)],
                ProjectProvenance = [.. selected.Where(_ => _.Provenance is not null).Select(_ => _.Provenance!)]
            };
    }
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
    public bool Matches(LoadedCompilation loaded) => loaded.Compilations.Any(_ =>
        ProviderEvidence.HasMarten(_) && ProviderEvidence.HasWolverine(_));
    public LoadedCompilation SelectFrom(LoadedCompilation loaded) => loaded;
    public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
        CritterStackScreenplayGeneration.GenerateFrom(loaded, targetPath, options);
}
