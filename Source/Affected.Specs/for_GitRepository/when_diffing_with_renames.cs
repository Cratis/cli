// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_GitRepository;

public class when_diffing_with_renames : Specification
{
    TemporaryRepository _repository;
    IReadOnlyList<FileChange> _changes;
    bool _ancestor;
    bool _notAncestor;
    bool _unknown;

    void Because()
    {
        _repository = new TemporaryRepository();
        var content = string.Join('\n', Enumerable.Range(0, 40).Select(i => $"line {i}"));
        _repository.Write("keep.txt", "keep");
        _repository.Write("edit.txt", "before");
        _repository.Write("gone.txt", "gone");
        _repository.Write("old/name.txt", content);
        var first = _repository.Commit("first");

        _repository.Write("edit.txt", "after");
        _repository.Delete("gone.txt");
        _repository.Move("old/name.txt", "new/name.txt");
        _repository.Write("fresh.txt", "fresh");
        var second = _repository.Commit("second");

        _changes = _repository.Repository.Diff(first, second);
        _ancestor = _repository.Repository.IsAncestor(first, second);
        _notAncestor = _repository.Repository.IsAncestor(second, first);
        _unknown = _repository.Repository.IsAncestor("0123456789abcdef0123456789abcdef01234567", second);
    }

    [Fact] void should_report_a_modification() => _changes.ShouldContain(new FileChange(ChangeKind.Modified, "edit.txt"));
    [Fact] void should_report_a_deletion() => _changes.ShouldContain(new FileChange(ChangeKind.Deleted, "gone.txt"));
    [Fact] void should_report_an_addition() => _changes.ShouldContain(new FileChange(ChangeKind.Added, "fresh.txt"));
    [Fact] void should_report_a_rename_with_its_old_path() => _changes.ShouldContain(new FileChange(ChangeKind.Renamed, "new/name.txt", "old/name.txt"));
    [Fact] void should_not_report_untouched_files() => _changes.Select(change => change.Path).ShouldNotContain("keep.txt");
    [Fact] void should_know_an_ancestor() => _ancestor.ShouldBeTrue();
    [Fact] void should_know_a_descendant_is_not_an_ancestor() => _notAncestor.ShouldBeFalse();
    [Fact] void should_treat_an_unknown_commit_as_not_an_ancestor() => _unknown.ShouldBeFalse();

    void Destroy() => _repository.Dispose();
}
