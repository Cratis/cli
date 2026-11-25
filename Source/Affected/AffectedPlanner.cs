// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Decides, for every test project of a repository, whether to run it in full, run a filtered set, or skip it.
/// Whatever goes wrong, the answer is to run more, and the planner still returns normally.
/// </summary>
/// <param name="git">The repository.</param>
/// <param name="runner">The test runner strategy.</param>
public class AffectedPlanner(IGitRepository git, ITestRunner runner)
{
    /// <summary>
    /// Plans a run.
    /// </summary>
    /// <param name="options">The inputs.</param>
    /// <param name="cancellationToken">Cancels the planning.</param>
    /// <returns>The plan.</returns>
    public async Task<SelectionReport> PlanAsync(PlanOptions options, CancellationToken cancellationToken = default)
    {
        AffectedConfiguration configuration;
        string to;
        ProjectGraph headGraph;
        try
        {
            configuration = AffectedConfiguration.Load(git.Root);
            to = git.ResolveCommit(options.To);
            headGraph = ProjectGraph.Load(git, to, configuration);
        }
        catch (Exception exception) when (exception is AffectedException or IOException)
        {
            return new SelectionReport(options.Mode, options.Level, options.From, options.To, true, [$"Could not read the repository, running everything: {exception.Message}"], [], []);
        }

        if (options.Mode != PlanMode.PullRequest || options.ForceAll)
        {
            var reason = options.ForceAll ? "a full run was requested" : $"{options.Mode} runs always run everything";
            return Everything(options, to, headGraph, [reason]);
        }

        AffectedResult affected;
        string baseCommit;
        try
        {
            baseCommit = git.MergeBase(options.From, to);
            var changes = git.Diff(baseCommit, to);
            affected = AffectedCalculator.Calculate(ProjectGraph.Load(git, baseCommit, configuration), headGraph, changes, configuration);
        }
        catch (Exception exception) when (exception is AffectedException or IOException)
        {
            return Everything(options, to, headGraph, [$"Could not compute the affected projects, running everything: {exception.Message}"]);
        }

        if (affected.RunEverything)
        {
            return Everything(options, to, headGraph, affected.RunEverythingReasons);
        }

        var plans = new List<ProjectPlan>();
        var affectedTests = affected.TestProjects.ToDictionary(project => project.Path, StringComparer.Ordinal);
        foreach (var project in headGraph.Projects.Values.Where(project => project.IsTestProject).OrderBy(project => project.Path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            plans.Add(affectedTests.TryGetValue(project.Path, out var test)
                ? await PlanProjectAsync(options, to, project, test, headGraph, configuration, cancellationToken)
                : new ProjectPlan(ProjectSelection.SkipAll(project.Path, "not affected by the change"), []));
        }

        var reasons = affected.ChangedProjects.Select(path => $"{path} changed")
            .Concat(affected.AddedProjects.Select(path => $"{path} added"))
            .Concat(affected.RemovedProjects.Select(path => $"{path} removed"))
            .ToList();
        return new SelectionReport(options.Mode, options.Level, baseCommit, to, false, reasons, affected.AffectedProjects, plans);
    }

    static SelectionReport Everything(PlanOptions options, string to, ProjectGraph headGraph, IReadOnlyList<string> reasons) =>
        new(
            options.Mode,
            options.Level,
            options.From,
            to,
            true,
            reasons,
            [.. headGraph.Projects.Keys.Order(StringComparer.Ordinal)],
            [.. headGraph.Projects.Values.Where(project => project.IsTestProject)
                .OrderBy(project => project.Path, StringComparer.Ordinal)
                .Select(project => new ProjectPlan(ProjectSelection.RunAll(project.Path, reasons.Count > 0 ? reasons[0] : "running everything"), []))]);

    async Task<ProjectPlan> PlanProjectAsync(
        PlanOptions options,
        string to,
        ProjectDefinition project,
        AffectedTestProject test,
        ProjectGraph graph,
        AffectedConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (test.RunAll)
        {
            return new ProjectPlan(ProjectSelection.RunAll(project.Path, test.Reason), []);
        }

        if (options.Level == SelectionLevel.Project)
        {
            return new ProjectPlan(ProjectSelection.RunAll(project.Path, "project-level selection: the project is affected"), []);
        }

        try
        {
            var map = CoverageMapFile.Read(git.Root, project.Path);
            if (map is null)
            {
                return new ProjectPlan(ProjectSelection.RunAll(project.Path, "no coverage map"), []);
            }

            if (map.Project != project.Path || !git.IsAncestor(map.BuiltAt, to))
            {
                return new ProjectPlan(ProjectSelection.RunAll(project.Path, $"the map was built at {map.BuiltAt}, which is not an ancestor of {to}"), []);
            }

            TestAssembly? assembly = null;
            IReadOnlyList<string>? discovered = null;
            if (options.Discover)
            {
                assembly = await runner.BuildAsync(project.Path, cancellationToken);
                discovered = await runner.DiscoverUnitsAsync(assembly, map.Granularity, cancellationToken);
            }

            var selection = TestSelector.Select(new TestSelectionInput(project, graph, map, git.Diff(map.BuiltAt, to), discovered, configuration, to));
            if (selection.Mode != SelectionMode.Filtered)
            {
                return new ProjectPlan(selection, [], assembly);
            }

            var batches = runner.CreateFilterBatches(selection.Entries, map.Granularity, configuration.MaxFilterLength);
            if (assembly is not null && discovered is not null)
            {
                var mismatch = await VerifyAsync(assembly, selection, batches, map.Granularity, discovered, cancellationToken);
                if (mismatch is not null)
                {
                    return new ProjectPlan(ProjectSelection.RunAll(project.Path, mismatch, discovered.Count), [], assembly);
                }
            }

            return new ProjectPlan(selection, batches, assembly);
        }
        catch (Exception exception) when (exception is AffectedException or IOException or InvalidOperationException)
        {
            return new ProjectPlan(ProjectSelection.RunAll(project.Path, $"selection failed, running everything: {exception.Message}"), []);
        }
    }

    async Task<string?> VerifyAsync(
        TestAssembly assembly,
        ProjectSelection selection,
        IReadOnlyList<IReadOnlyList<string>> batches,
        UnitGranularity granularity,
        IReadOnlyList<string> discovered,
        CancellationToken cancellationToken)
    {
        var matched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var batch in batches)
        {
            matched.UnionWith(await runner.ListMatchedAsync(assembly, batch, granularity, cancellationToken));
        }

        var expected = discovered.Where(unit => selection.Entries.Any(entry => entry.Selects(unit, granularity))).ToHashSet(StringComparer.Ordinal);
        return SelectionVerifier.Compare(expected, matched);
    }
}
