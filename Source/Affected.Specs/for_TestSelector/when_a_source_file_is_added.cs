// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_a_source_file_is_added : Specification
{
    ProjectSelection _project;
    ProjectSelection _directory;
    ProjectSelection _ignore;

    void Because()
    {
        var map = Maps.For(
            "base",
            ("Foo.Specs.A.when_a", ["Source/Foo/Sub/A.cs"]),
            ("Foo.Specs.B.when_b", ["Source/Foo/Sub/B.cs"]),
            ("Foo.Specs.C.when_c", ["Source/Foo/Other/C.cs"]),
            ("Foo.Specs.D.when_d", ["Source/Foo.Specs/Helper.cs"]));
        var added = new[] { new FileChange(ChangeKind.Added, "Source/Foo/Sub/New.cs") };
        _project = Maps.Select(map, added, new AffectedConfiguration { AddedFilePolicy = AddedFilePolicy.Project });
        _directory = Maps.Select(map, added, new AffectedConfiguration { AddedFilePolicy = AddedFilePolicy.Directory });
        _ignore = Maps.Select(map, added, new AffectedConfiguration { AddedFilePolicy = AddedFilePolicy.Ignore });
    }

    [Fact] void should_select_every_unit_covering_the_same_project_by_default() => _project.Entries.Select(entry => entry.Value).ShouldContainOnly("Foo.Specs.A.when_a", "Foo.Specs.B.when_b", "Foo.Specs.C.when_c");
    [Fact] void should_select_only_units_covering_the_same_directory_when_narrowed() => _directory.Entries.Select(entry => entry.Value).ShouldContainOnly("Foo.Specs.A.when_a", "Foo.Specs.B.when_b");
    [Fact] void should_select_nothing_when_ignored() => _ignore.Mode.ShouldEqual(SelectionMode.Skip);
}
