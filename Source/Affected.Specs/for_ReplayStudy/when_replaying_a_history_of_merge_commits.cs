// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_ReplayStudy;

public class when_replaying_a_history_of_merge_commits : Specification
{
    PlannerRepository _repository;
    ReplayReport _report;
    string _merge;

    void Because()
    {
        _repository = new PlannerRepository();
        _repository.WriteMap(_repository.BaseCommit);
        _repository.Repository.Commit("map");
        _repository.Repository.Git("checkout", "-q", "-b", "feature");
        _repository.Repository.Write("Source/Foo/A.cs", "namespace Foo; public class A { public int X; }");
        _repository.Repository.Commit("change A");
        _repository.Repository.Git("checkout", "-q", "main");
        _repository.Repository.Git("merge", "--no-ff", "-q", "-m", "Merge pull request", "feature");
        _merge = _repository.Repository.Git("rev-parse", "HEAD").Trim();
        _repository.WriteMap(_merge);
        _repository.Repository.Commit("Update coverage maps");

        _report = new ReplayStudy(_repository.Repository.Repository).Run("main", 5, new Dictionary<string, IReadOnlyList<string>>());
    }

    [Fact] void should_replay_the_merge_commit_as_one_change() => _report.Commits.Select(commit => commit.Commit).ShouldContainOnly(_merge);
    [Fact] void should_not_replay_the_commit_that_only_refreshes_the_maps() => _report.Commits.Count.ShouldEqual(1);
    [Fact] void should_select_the_units_the_merged_change_reaches() => _report.Commits[0].SelectedFraction.ShouldEqual(0.5);

    void Destroy() => _repository.Dispose();
}
