// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Replays already merged changes offline, against the coverage maps committed before each of them, to measure how much of the
/// test suite the selection would have run and whether it would have missed a test that really failed.
/// </summary>
/// <param name="git">The repository.</param>
public class ReplayStudy(IGitRepository git)
{
    /// <summary>
    /// Replays the most recent changes of a branch. Each first-parent commit is one change and its first parent is the base, so a merge
    /// commit is the change it brought in, whichever way the repository merges. A commit that only refreshes the coverage maps is not a change.
    /// </summary>
    /// <param name="branch">The branch to read history from.</param>
    /// <param name="count">How many changes to replay.</param>
    /// <param name="failedUnits">For a commit, the units that failed in the real full run of that change; may be empty.</param>
    /// <returns>The report.</returns>
    public ReplayReport Run(string branch, int count, IReadOnlyDictionary<string, IReadOnlyList<string>> failedUnits)
    {
        var configuration = AffectedConfiguration.Load(git.Root);
        var commits = ProcessRunner.Run("git", ["rev-list", "--first-parent", "--min-parents=1", branch], git.Root)
            .Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var replayed = new List<ReplayCommit>();

        foreach (var commit in commits)
        {
            if (replayed.Count >= count)
            {
                break;
            }

            var parent = git.ResolveCommit(commit + "^");
            var changes = git.Diff(parent, commit);
            if (changes.All(change => change.Paths.All(IsCoverageMap)))
            {
                continue;
            }

            replayed.Add(Replay(commit, parent, changes, configuration, failedUnits.TryGetValue(commit, out var failed) ? failed : []));
        }

        return new ReplayReport(replayed);
    }

    static bool IsCoverageMap(string path) => path.StartsWith(AffectedConfiguration.MapsFolder + "/", StringComparison.Ordinal);

    static string? KnownUnit(CoverageMap map, string unit)
    {
        if (map.Units.Any(candidate => candidate.Name == unit))
        {
            return unit;
        }

        var className = UnitNames.ClassOf(unit);
        return map.Units.Any(candidate => candidate.Name == className) ? className : null;
    }

    static bool IsSelected(string unit, List<(ProjectSelection Selection, CoverageMap? Map)> selections)
    {
        foreach (var (selection, map) in selections)
        {
            if (map is null)
            {
                // Without a map the project's units are unknown; a full run of it is the only thing that surely includes the unit.
                if (selection.Mode == SelectionMode.All)
                {
                    return true;
                }

                continue;
            }

            var knownUnit = KnownUnit(map, unit);
            if (knownUnit is null)
            {
                continue;
            }

            if (selection.Mode == SelectionMode.All || (selection.Mode == SelectionMode.Filtered && selection.Entries.Any(entry => entry.Selects(knownUnit, map.Granularity))))
            {
                return true;
            }
        }

        return false;
    }

    ReplayCommit Replay(string commit, string parent, IReadOnlyList<FileChange> changes, AffectedConfiguration configuration, IReadOnlyList<string> failed)
    {
        var baseGraph = ProjectGraph.Load(git, parent, configuration);
        var headGraph = ProjectGraph.Load(git, commit, configuration);
        var affected = AffectedCalculator.Calculate(baseGraph, headGraph, changes, configuration);
        var tests = headGraph.Projects.Values.Where(project => project.IsTestProject).ToList();
        var byProject = affected.TestProjects.ToDictionary(project => project.Path, StringComparer.Ordinal);

        var selections = new List<(ProjectSelection Selection, CoverageMap? Map)>();
        foreach (var project in tests)
        {
            if (!byProject.TryGetValue(project.Path, out var test))
            {
                selections.Add((ProjectSelection.SkipAll(project.Path, "not affected"), ReadMap(parent, project.Path)));
                continue;
            }

            var map = ReadMap(parent, project.Path);
            if (test.RunAll || map is null)
            {
                selections.Add((ProjectSelection.RunAll(project.Path, test.Reason), map));
                continue;
            }

            var sinceMap = git.IsAncestor(map.BuiltAt, commit) ? git.Diff(map.BuiltAt, commit) : null;
            selections.Add((sinceMap is null
                ? ProjectSelection.RunAll(project.Path, "map is not an ancestor")
                : TestSelector.Select(new TestSelectionInput(project, headGraph, map, sinceMap, null, configuration, commit)), map));
        }

        var selected = selections.Where(item => item.Map is not null).Sum(item => item.Selection.Mode == SelectionMode.All ? item.Map!.Units.Count : item.Selection.SelectedUnits);
        var total = selections.Where(item => item.Map is not null).Sum(item => item.Map!.Units.Count);
        var misses = failed.Where(unit => !IsSelected(unit, selections)).Order(StringComparer.Ordinal).ToList();

        return new ReplayCommit(
            commit,
            affected.RunEverything,
            tests.Count,
            selections.Count(item => item.Selection.Mode != SelectionMode.Skip),
            selected,
            total,
            misses);
    }

    CoverageMap? ReadMap(string revision, string project)
    {
        var text = git.ReadFile(revision, CoverageMapFile.PathFor(project));
        if (text is null)
        {
            return null;
        }

        try
        {
            return CoverageMapFile.Parse(text);
        }
        catch (AffectedException)
        {
            return null;
        }
    }
}
