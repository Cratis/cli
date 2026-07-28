// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Selects which project of a loaded solution the Screenplay is generated from.
/// </summary>
/// <remarks>
/// A Screenplay describes one application, so a solution has to narrow down to a single project. Spec projects are
/// dropped, and when any project produces an executable the libraries are dropped too — an Arc application is the
/// executable. Anything still ambiguous is reported rather than guessed at.
/// </remarks>
public static class ScreenplayProjectSelection
{
    static readonly string[] _specSuffixes = [".Specs", ".Specifications", ".Tests", ".Test", ".IntegrationTests"];

    /// <summary>
    /// Selects the single project to generate from.
    /// </summary>
    /// <param name="candidates">The projects found in the solution.</param>
    /// <returns>The name of the selected project, or <see langword="null"/> when the choice is ambiguous or there is nothing to choose from.</returns>
    public static string? Select(IEnumerable<ScreenplayProjectCandidate> candidates)
    {
        var remaining = Narrow(candidates);
        return remaining.Count == 1 ? remaining[0].Name : null;
    }

    /// <summary>
    /// Narrows the projects down to the ones a Screenplay could reasonably be generated from.
    /// </summary>
    /// <param name="candidates">The projects found in the solution.</param>
    /// <returns>The remaining projects, ordered by name.</returns>
    public static IReadOnlyList<ScreenplayProjectCandidate> Narrow(IEnumerable<ScreenplayProjectCandidate> candidates)
    {
        var withoutSpecs = candidates
            .Where(candidate => !IsSpecProject(candidate.Name))
            .OrderBy(candidate => candidate.Name, StringComparer.Ordinal)
            .ToArray();

        var executables = withoutSpecs.Where(candidate => candidate.IsExecutable).ToArray();
        return executables.Length > 0 ? executables : withoutSpecs;
    }

    /// <summary>
    /// Determines whether the given project name identifies a spec or test project.
    /// </summary>
    /// <param name="name">The project name.</param>
    /// <returns><see langword="true"/> when the project holds specs or tests.</returns>
    public static bool IsSpecProject(string name) =>
        Array.Exists(_specSuffixes, suffix => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}
