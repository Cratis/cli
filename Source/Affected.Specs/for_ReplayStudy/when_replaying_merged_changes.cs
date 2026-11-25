// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_ReplayStudy;

public class when_replaying_merged_changes : Specification
{
    PlannerRepository _repository;
    ReplayReport _report;
    ReplayReport _withMiss;
    string _changeA;
    string _changeB;

    void Because()
    {
        _repository = new PlannerRepository();
        _repository.WriteMap(_repository.BaseCommit);
        _repository.Repository.Commit("map");
        _repository.Repository.Write("Source/Foo/A.cs", "namespace Foo; public class A { public int X; }");
        _changeA = _repository.Repository.Commit("change A");
        _repository.Repository.Write("Source/Other/Thing.cs", "namespace Other; public class Thing { }");
        _changeB = _repository.Repository.Commit("change Other");

        var study = new ReplayStudy(_repository.Repository.Repository);
        _report = study.Run("main", 2, new Dictionary<string, IReadOnlyList<string>>());
        _withMiss = study.Run("main", 2, new Dictionary<string, IReadOnlyList<string>>
        {
            [_changeA] = ["Foo.Specs.when_b.should_fail"]
        });
    }

    [Fact] void should_replay_each_change() => _report.Commits.Count.ShouldEqual(2);
    [Fact] void should_select_one_of_two_units_for_the_covered_change() => _report.Commits.Single(commit => commit.Commit == _changeA).SelectedFraction.ShouldEqual(0.5);
    [Fact] void should_select_nothing_for_the_unrelated_change() => _report.Commits.Single(commit => commit.Commit == _changeB).SelectedUnits.ShouldEqual(0);
    [Fact] void should_report_no_misses_without_failures() => _report.MissedChanges.ShouldEqual(0);
    [Fact] void should_report_a_miss_when_a_failing_test_was_not_selected() => _withMiss.MissedChanges.ShouldEqual(1);
    [Fact] void should_report_the_median_selected_fraction() => _report.MedianSelectedFraction.ShouldEqual(0.25);
    [Fact] void should_render_markdown() => _report.ToMarkdown().ShouldContain("Median selected fraction");

    void Destroy() => _repository.Dispose();
}
