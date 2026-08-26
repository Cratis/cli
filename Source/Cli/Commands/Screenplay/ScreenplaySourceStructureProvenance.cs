// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the shared source-structure policy applied to one project.
/// </summary>
/// <param name="ProjectRole">The semantic project role.</param>
/// <param name="PolicyVersion">The source-structure policy version.</param>
/// <param name="FeatureRoot">The optional project-relative feature root.</param>
/// <param name="Module">The optional module override.</param>
/// <param name="NamespaceSegmentsToSkip">The number of leading namespace segments skipped.</param>
public record ScreenplaySourceStructureProvenance(
    string ProjectRole,
    int PolicyVersion,
    string? FeatureRoot,
    string? Module,
    int NamespaceSegmentsToSkip);
