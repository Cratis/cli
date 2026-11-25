// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedPlanner;

public class when_the_map_cannot_be_trusted : Specification
{
    ProjectSelection _missing;
    ProjectSelection _corrupted;
    ProjectSelection _unknownSchema;
    ProjectSelection _notAnAncestor;
    ProjectSelection _mismatch;
    string _commitWithMap;

    async Task Because()
    {
        _missing = await Plan(repository => { });
        _corrupted = await Plan(repository => repository.WriteMap(repository.BaseCommit, "{ this is not a map"));
        _unknownSchema = await Plan(repository => repository.WriteMap(repository.BaseCommit, "{\"schemaVersion\":42}"));
        _notAnAncestor = await Plan(repository => repository.WriteMap("0123456789abcdef0123456789abcdef01234567"));
        _mismatch = await Plan(repository => repository.WriteMap(repository.BaseCommit), matchWrongly: true);
        _commitWithMap = string.Empty;
    }

    [Fact] void should_run_all_without_a_map() => _missing.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_say_there_is_no_map() => _missing.Reasons.Single().ShouldContain("no coverage map");
    [Fact] void should_run_all_with_a_corrupted_map() => _corrupted.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_run_all_with_an_unknown_schema_version() => _unknownSchema.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_run_all_when_the_map_commit_is_not_an_ancestor() => _notAnAncestor.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_explain_the_ancestry_problem() => _notAnAncestor.Reasons.Single().ShouldContain("not an ancestor");
    [Fact] void should_run_all_when_the_filter_does_not_match_the_selection() => _mismatch.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_say_the_filter_mismatched() => _mismatch.Reasons.Single().ShouldContain("does not match the selection");

    static async Task<ProjectSelection> Plan(Action<PlannerRepository> arrange, bool matchWrongly = false)
    {
        using var repository = new PlannerRepository();
        arrange(repository);
        repository.Repository.Commit("map");
        repository.Repository.Git("checkout", "-q", "-b", "pr");
        repository.Repository.Write("Source/Foo/A.cs", "namespace Foo; public class A { public int X; }");
        repository.Repository.Commit("change A");

        var planner = new AffectedPlanner(repository.Repository.Repository, new FakeTestRunner(repository.Units, matchWrongly));
        var report = await planner.PlanAsync(new PlanOptions("main", "HEAD"));
        return report.Projects.Single(plan => plan.Selection.Project == Maps.TestProject).Selection;
    }
}
