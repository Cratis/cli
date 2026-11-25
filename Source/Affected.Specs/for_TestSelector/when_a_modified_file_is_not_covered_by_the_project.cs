// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_a_modified_file_is_not_covered_by_the_project : Specification
{
    ProjectSelection _project;
    ProjectSelection _directory;
    ProjectSelection _ignore;
    ProjectSelection _addedStillFollowsItsOwnPolicy;

    void Because()
    {
        var map = Maps.For(
            "base",
            ("Foo.Specs.A.when_a", ["Source/Foo/Sub/A.cs"]),
            ("Foo.Specs.B.when_b", ["Source/Foo/Sub/B.cs"]),
            ("Foo.Specs.C.when_c", ["Source/Foo/Other/C.cs"]));
        var modified = new[] { new FileChange(ChangeKind.Modified, "Source/Foo/Sub/Untouched.cs") };
        _project = Maps.Select(map, modified, new AffectedConfiguration { UnmappedFilePolicy = AddedFilePolicy.Project });
        _directory = Maps.Select(map, modified, new AffectedConfiguration { UnmappedFilePolicy = AddedFilePolicy.Directory });
        _ignore = Maps.Select(map, modified, new AffectedConfiguration { UnmappedFilePolicy = AddedFilePolicy.Ignore });
        _addedStillFollowsItsOwnPolicy = Maps.Select(
            map,
            [new FileChange(ChangeKind.Added, "Source/Foo/Sub/New.cs")],
            new AffectedConfiguration { UnmappedFilePolicy = AddedFilePolicy.Ignore });
    }

    [Fact] void should_select_every_unit_covering_the_same_project_by_default() => _project.Mode.ShouldNotEqual(SelectionMode.Skip);
    [Fact] void should_select_only_units_covering_the_same_directory_when_narrowed() => _directory.Entries.Select(entry => entry.Value).ShouldContainOnly("Foo.Specs.A.when_a", "Foo.Specs.B.when_b");
    [Fact] void should_select_nothing_when_ignored() => _ignore.Mode.ShouldEqual(SelectionMode.Skip);
    [Fact] void should_not_let_the_policy_for_modified_files_decide_for_added_files() => _addedStillFollowsItsOwnPolicy.Mode.ShouldNotEqual(SelectionMode.Skip);
}
