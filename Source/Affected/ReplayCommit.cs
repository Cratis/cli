// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The replayed selection of one merged change.
/// </summary>
/// <param name="Commit">The merged commit.</param>
/// <param name="RunEverything">Whether the change forced a full run.</param>
/// <param name="TestProjects">How many test projects the repository has.</param>
/// <param name="ProjectsRun">How many test projects the selection runs at all.</param>
/// <param name="SelectedUnits">The units the selection runs, over projects with a map.</param>
/// <param name="TotalUnits">The units of those projects.</param>
/// <param name="Misses">Failed units of the real run that the selection did not include.</param>
public record ReplayCommit(
    string Commit,
    bool RunEverything,
    int TestProjects,
    int ProjectsRun,
    int SelectedUnits,
    int TotalUnits,
    IReadOnlyList<string> Misses)
{
    /// <summary>
    /// Gets the fraction of units the selection runs, or 1 when there are no mapped units.
    /// </summary>
    public double SelectedFraction => TotalUnits == 0 ? 1 : (double)SelectedUnits / TotalUnits;
}
