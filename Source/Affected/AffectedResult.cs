// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// What a set of changes reaches at project level.
/// </summary>
/// <param name="RunEverything">Whether the change forces a full build and test run.</param>
/// <param name="RunEverythingReasons">Why it does.</param>
/// <param name="ChangedProjects">The projects with a changed file.</param>
/// <param name="AddedProjects">The projects that did not exist before.</param>
/// <param name="RemovedProjects">The projects that no longer exist.</param>
/// <param name="AffectedProjects">The changed and added projects and everything that depends on them.</param>
/// <param name="TestProjects">The affected test projects.</param>
public record AffectedResult(
    bool RunEverything,
    IReadOnlyList<string> RunEverythingReasons,
    IReadOnlyList<string> ChangedProjects,
    IReadOnlyList<string> AddedProjects,
    IReadOnlyList<string> RemovedProjects,
    IReadOnlyList<string> AffectedProjects,
    IReadOnlyList<AffectedTestProject> TestProjects);
