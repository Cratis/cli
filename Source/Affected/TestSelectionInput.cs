// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Everything the test selector needs to decide for one test project.
/// </summary>
/// <param name="Project">The test project.</param>
/// <param name="Graph">The project graph at the commit being tested.</param>
/// <param name="Map">The coverage map of the project.</param>
/// <param name="ChangesSinceMap">The file changes from the commit the map was built at up to the commit being tested.</param>
/// <param name="DiscoveredUnits">The units the built project currently has, or null when they were not discovered.</param>
/// <param name="Configuration">The repository configuration.</param>
/// <param name="SampleSeed">A value that varies the canary sample from commit to commit.</param>
public record TestSelectionInput(
    ProjectDefinition Project,
    ProjectGraph Graph,
    CoverageMap Map,
    IReadOnlyList<FileChange> ChangesSinceMap,
    IReadOnlyCollection<string>? DiscoveredUnits,
    AffectedConfiguration Configuration,
    string SampleSeed);
