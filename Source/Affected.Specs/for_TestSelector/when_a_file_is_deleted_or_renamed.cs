// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_a_file_is_deleted_or_renamed : Specification
{
    ProjectSelection _deleted;
    ProjectSelection _renamed;

    void Because()
    {
        var map = Maps.For(
            "base",
            ("Foo.Specs.A.when_a", ["Source/Foo/A.cs"]),
            ("Foo.Specs.B.when_b", ["Source/Foo/B.cs"]),
            ("Foo.Specs.C.when_c", ["Source/Foo/C.cs"]));
        var configuration = new AffectedConfiguration { AddedFilePolicy = AddedFilePolicy.Ignore };
        _deleted = Maps.Select(map, [new FileChange(ChangeKind.Deleted, "Source/Foo/A.cs")], configuration);
        _renamed = Maps.Select(map, [new FileChange(ChangeKind.Renamed, "Source/Foo/Moved.cs", "Source/Foo/B.cs")], configuration);
    }

    [Fact] void should_select_the_units_that_covered_the_deleted_file() => _deleted.Entries.Single().Value.ShouldEqual("Foo.Specs.A.when_a");
    [Fact] void should_select_the_units_that_covered_the_renamed_from_file() => _renamed.Entries.Single().Value.ShouldEqual("Foo.Specs.B.when_b");
}
