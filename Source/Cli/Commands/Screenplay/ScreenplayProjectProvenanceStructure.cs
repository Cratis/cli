// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Adds the CLI-owned shared source-structure policy to loaded project provenance.
/// </summary>
static class ScreenplayProjectProvenanceStructure
{
    /// <summary>
    /// Adds aligned project roles and the options the selected provider actually supports.
    /// </summary>
    /// <param name="loaded">The selected loaded projects.</param>
    /// <param name="options">The validated generation options.</param>
    /// <param name="supportsFeatureRoot">Whether the selected provider applies the validated feature root.</param>
    /// <returns>The loaded projects carrying source-structure provenance.</returns>
    internal static LoadedCompilation Apply(
        LoadedCompilation loaded,
        ScreenplayGenerationOptions options,
        bool supportsFeatureRoot)
    {
        if (loaded.ProjectProvenance.Count == 0)
        {
            return loaded;
        }

        var policy = new DotNetSourceStructurePolicy
        {
            FeatureRoot = supportsFeatureRoot ? options.FeatureRoot : null,
            Module = options.Module,
            NamespaceSegmentsToSkip = options.SegmentsToSkip ?? 0
        };
        return loaded with
        {
            ProjectProvenance =
            [
                .. loaded.ProjectProvenance.Select((project, index) => project with
                {
                    SourceStructure = new ScreenplaySourceStructureProvenance(
                        loaded.ProjectSources.Count > index
                            ? loaded.ProjectSources[index].Role.ToString()
                            : nameof(DotNetProjectRole.Application),
                        policy.Version,
                        policy.FeatureRoot,
                        policy.Module,
                        policy.NamespaceSegmentsToSkip)
                })
            ]
        };
    }
}
