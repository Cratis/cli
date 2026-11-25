// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The decision for one test project.
/// </summary>
/// <param name="Project">The test project file path.</param>
/// <param name="Mode">How the project is run.</param>
/// <param name="Entries">The selected entries when <paramref name="Mode"/> is <see cref="SelectionMode.Filtered"/>.</param>
/// <param name="SelectedUnits">How many units run.</param>
/// <param name="TotalUnits">How many units the project has.</param>
/// <param name="Reasons">Why, in the order they were found.</param>
public record ProjectSelection(
    string Project,
    SelectionMode Mode,
    IReadOnlyList<SelectionEntry> Entries,
    int SelectedUnits,
    int TotalUnits,
    IReadOnlyList<string> Reasons)
{
    /// <summary>
    /// Creates a decision to run every test.
    /// </summary>
    /// <param name="project">The test project file path.</param>
    /// <param name="reason">Why everything runs.</param>
    /// <param name="totalUnits">How many units the project has, when known.</param>
    /// <returns>The selection.</returns>
    public static ProjectSelection RunAll(string project, string reason, int totalUnits = 0) =>
        new(project, SelectionMode.All, [], totalUnits, totalUnits, [reason]);

    /// <summary>
    /// Creates a decision to run nothing.
    /// </summary>
    /// <param name="project">The test project file path.</param>
    /// <param name="reason">Why nothing runs.</param>
    /// <param name="totalUnits">How many units the project has, when known.</param>
    /// <returns>The selection.</returns>
    public static ProjectSelection SkipAll(string project, string reason, int totalUnits = 0) =>
        new(project, SelectionMode.Skip, [], 0, totalUnits, [reason]);
}
