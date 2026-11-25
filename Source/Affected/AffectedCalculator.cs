// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Decides which projects a set of file changes reaches. Every doubt resolves to more work, never less.
/// </summary>
public static class AffectedCalculator
{
    /// <summary>
    /// Calculates the affected projects.
    /// </summary>
    /// <param name="baseGraph">The projects before the change; needed to resolve removed and moved projects.</param>
    /// <param name="headGraph">The projects after the change.</param>
    /// <param name="changes">The file changes between the two.</param>
    /// <param name="configuration">The repository configuration.</param>
    /// <returns>What the change reaches.</returns>
    public static AffectedResult Calculate(ProjectGraph baseGraph, ProjectGraph headGraph, IReadOnlyList<FileChange> changes, AffectedConfiguration configuration)
    {
        var added = headGraph.Projects.Keys.Except(baseGraph.Projects.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var removed = baseGraph.Projects.Keys.Except(headGraph.Projects.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var relevant = changes.Where(change => !change.Paths.All(configuration.IsIgnored)).ToList();

        var everythingReasons = FindRunEverythingReasons(relevant, baseGraph, headGraph, configuration);
        if (everythingReasons.Count > 0)
        {
            return Everything(headGraph, everythingReasons, added, removed);
        }

        var changed = FindChangedProjects(relevant, baseGraph, headGraph);
        var seeds = changed.Union(added, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        // A project removed from the base graph can no longer be built against, so whatever depended on it is affected.
        var survivingDependents = baseGraph.DependentsOf(removed).Where(headGraph.Projects.ContainsKey).ToList();
        var roots = seeds.Union(survivingDependents, StringComparer.Ordinal).ToList();
        var affected = roots.Union(headGraph.DependentsOf(roots), StringComparer.Ordinal).Where(headGraph.Projects.ContainsKey).ToHashSet(StringComparer.Ordinal);

        var generators = affected.Where(path => headGraph.Projects[path].IsGenerator)
            .Union(removed.Where(path => baseGraph.Projects[path].IsGenerator), StringComparer.Ordinal)
            .ToList();
        var generatorDependents = headGraph.DependentsOf(generators);

        var tests = new List<AffectedTestProject>();
        foreach (var path in affected.Where(path => headGraph.Projects[path].IsTestProject).Order(StringComparer.Ordinal))
        {
            if (added.Contains(path))
            {
                tests.Add(new AffectedTestProject(path, true, "new test project"));
            }
            else if (generatorDependents.Contains(path) || generators.Contains(path))
            {
                tests.Add(new AffectedTestProject(path, true, "depends on a changed source generator or analyzer"));
            }
            else
            {
                tests.Add(new AffectedTestProject(path, false, "affected by a changed project"));
            }
        }

        return new AffectedResult(
            false,
            [],
            [.. changed.Order(StringComparer.Ordinal)],
            added,
            removed,
            [.. affected.Order(StringComparer.Ordinal)],
            tests);
    }

    static List<string> FindRunEverythingReasons(List<FileChange> changes, ProjectGraph baseGraph, ProjectGraph headGraph, AffectedConfiguration configuration)
    {
        var reasons = new List<string>();
        foreach (var path in changes.SelectMany(change => change.Paths).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (configuration.IsRunEverythingTrigger(path))
            {
                reasons.Add($"{path} is a run-everything trigger");
            }
            else if (headGraph.OwnersOf(path).Count == 0 && baseGraph.OwnersOf(path).Count == 0)
            {
                reasons.Add($"{path} is outside every project directory and not mapped to another job");
            }
        }

        return reasons;
    }

    static HashSet<string> FindChangedProjects(List<FileChange> changes, ProjectGraph baseGraph, ProjectGraph headGraph)
    {
        var changed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in changes)
        {
            if (change.Kind != ChangeKind.Deleted)
            {
                changed.UnionWith(headGraph.OwnersOf(change.Path));
            }

            var lost = change.Kind == ChangeKind.Deleted ? change.Path : change.OldPath;
            if (lost is not null)
            {
                changed.UnionWith(baseGraph.OwnersOf(lost).Where(headGraph.Projects.ContainsKey));
            }
        }

        changed.RemoveWhere(path => !headGraph.Projects.ContainsKey(path));
        return changed;
    }

    static AffectedResult Everything(ProjectGraph headGraph, List<string> reasons, List<string> added, List<string> removed) =>
        new(
            true,
            reasons,
            [],
            added,
            removed,
            [.. headGraph.Projects.Keys.Order(StringComparer.Ordinal)],
            [.. headGraph.Projects.Values.Where(project => project.IsTestProject).OrderBy(project => project.Path, StringComparer.Ordinal).Select(project => new AffectedTestProject(project.Path, true, reasons[0]))]);
}
