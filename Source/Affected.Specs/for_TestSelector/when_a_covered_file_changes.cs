// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_a_covered_file_changes : Specification
{
    ProjectSelection _result;

    void Because()
    {
        var map = Maps.For(
            "base",
            ("Foo.Specs.A.when_a", ["Source/Foo/A.cs"]),
            ("Foo.Specs.B.when_b", ["Source/Foo/B.cs"]),
            ("Foo.Specs.C.when_c", ["Source/Foo/B.cs", "Source/Foo/C.cs"]),
            ("Foo.Specs.D.when_d", ["Source/Foo/D.cs"]));
        _result = Maps.Select(map, Maps.Modified("Source/Foo/B.cs"));
    }

    [Fact] void should_run_a_filtered_set() => _result.Mode.ShouldEqual(SelectionMode.Filtered);
    [Fact] void should_select_every_unit_that_covers_the_file() => _result.Entries.Select(entry => entry.Value).ShouldContainOnly("Foo.Specs.B.when_b", "Foo.Specs.C.when_c");
    [Fact] void should_count_the_selected_units() => _result.SelectedUnits.ShouldEqual(2);
    [Fact] void should_count_all_units() => _result.TotalUnits.ShouldEqual(4);
    [Fact] void should_explain_the_selection() => _result.Reasons[0].ShouldContain("Source/Foo/B.cs");
}
