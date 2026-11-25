// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedPlanner;

public class when_the_selection_is_overridden : Specification
{
    SelectionReport _forced;
    SelectionReport _merge;
    SelectionReport _projectLevel;
    SelectionReport _globalChange;
    SelectionReport _notARepository;

    async Task Because()
    {
        using var repository = new PlannerRepository();
        repository.WriteMap(repository.BaseCommit);
        repository.Repository.Commit("map");
        repository.Repository.Git("checkout", "-q", "-b", "pr");
        repository.Repository.Write("Source/Foo/A.cs", "namespace Foo; public class A { public int X; }");
        repository.Repository.Commit("change A");

        var planner = new AffectedPlanner(repository.Repository.Repository, new FakeTestRunner(repository.Units));
        _forced = await planner.PlanAsync(new PlanOptions("main", "HEAD", ForceAll: true));
        _merge = await planner.PlanAsync(new PlanOptions("main", "HEAD", PlanMode.Merge));
        _projectLevel = await planner.PlanAsync(new PlanOptions("main", "HEAD", Level: SelectionLevel.Project));

        repository.Repository.Write("Directory.Packages.props", "<Project />");
        repository.Repository.Commit("global");
        _globalChange = await planner.PlanAsync(new PlanOptions("main", "HEAD"));

        _notARepository = await planner.PlanAsync(new PlanOptions("does-not-exist", "also-missing"));
    }

    [Fact] void should_run_everything_when_forced() => _forced.Projects.ShouldEachConformTo(plan => plan.Selection.Mode == SelectionMode.All);
    [Fact] void should_run_everything_on_merge() => _merge.Projects.ShouldEachConformTo(plan => plan.Selection.Mode == SelectionMode.All);
    [Fact] void should_run_whole_affected_projects_at_project_level() => _projectLevel.Projects.Single(plan => plan.Selection.Project == Maps.TestProject).Selection.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_skip_unaffected_projects_at_project_level() => _projectLevel.Projects.Single(plan => plan.Selection.Project != Maps.TestProject).Selection.Mode.ShouldEqual(SelectionMode.Skip);
    [Fact] void should_run_everything_for_a_global_file() => _globalChange.RunEverything.ShouldBeTrue();
    [Fact] void should_still_return_a_plan_when_refs_cannot_be_resolved() => _notARepository.RunEverything.ShouldBeTrue();
}
