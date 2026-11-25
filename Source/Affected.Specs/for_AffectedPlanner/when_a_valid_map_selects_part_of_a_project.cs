// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedPlanner;

public class when_a_valid_map_selects_part_of_a_project : Specification
{
    PlannerRepository _repository;
    SelectionReport _report;
    ProjectPlan _foo;
    ProjectPlan _other;

    async Task Because()
    {
        _repository = new PlannerRepository();
        _repository.WriteMap(_repository.BaseCommit);
        _repository.Repository.Commit("map");
        _repository.Repository.Git("checkout", "-q", "-b", "pr");
        _repository.Repository.Write("Source/Foo/A.cs", "namespace Foo; public class A { public int X; }");
        _repository.Repository.Commit("change A");

        var planner = new AffectedPlanner(_repository.Repository.Repository, new FakeTestRunner(_repository.Units));
        _report = await planner.PlanAsync(new PlanOptions("main", "HEAD"));
        _foo = _report.Projects.Single(plan => plan.Selection.Project == Maps.TestProject);
        _other = _report.Projects.Single(plan => plan.Selection.Project != Maps.TestProject);
    }

    [Fact] void should_not_run_everything() => _report.RunEverything.ShouldBeFalse();
    [Fact] void should_filter_the_affected_project() => _foo.Selection.Mode.ShouldEqual(SelectionMode.Filtered);
    [Fact] void should_select_the_test_that_covers_the_changed_file() => _foo.Selection.Entries.Single().Value.ShouldEqual("Foo.Specs.when_a");
    [Fact] void should_hand_the_runner_a_filter() => string.Join(' ', _foo.FilterBatches.Single()).ShouldEqual("-filter /*/Foo.Specs/when_a/*");
    [Fact] void should_skip_the_unreached_project() => _other.Selection.Mode.ShouldEqual(SelectionMode.Skip);

    void Destroy() => _repository.Dispose();
}
