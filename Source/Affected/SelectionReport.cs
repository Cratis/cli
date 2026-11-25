// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// What was selected and why, for people to read and for later steps to act on.
/// </summary>
/// <param name="Mode">The situation the tests run in.</param>
/// <param name="Level">How finely tests were selected.</param>
/// <param name="From">The commit the change was measured against.</param>
/// <param name="To">The commit tested.</param>
/// <param name="RunEverything">Whether a full run was forced.</param>
/// <param name="Reasons">Why a full run was forced, or what was affected.</param>
/// <param name="AffectedProjects">Every affected project.</param>
/// <param name="Projects">The decision per test project.</param>
public record SelectionReport(
    PlanMode Mode,
    SelectionLevel Level,
    string From,
    string To,
    bool RunEverything,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> AffectedProjects,
    IReadOnlyList<ProjectPlan> Projects);
