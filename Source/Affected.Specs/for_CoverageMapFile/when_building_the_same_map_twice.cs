// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_CoverageMapFile;

public class when_building_the_same_map_twice : Specification
{
    string _first;
    string _second;
    CoverageMap _parsed;

    void Because()
    {
        _first = CoverageMapFile.Serialize(Build(shuffled: false));
        _second = CoverageMapFile.Serialize(Build(shuffled: true));
        _parsed = CoverageMapFile.Parse(_first);
    }

    [Fact] void should_produce_byte_identical_text() => _second.ShouldEqual(_first);
    [Fact] void should_not_contain_a_timestamp() => _first.ShouldNotContain("20");
    [Fact] void should_end_with_a_newline() => _first.EndsWith('\n').ShouldBeTrue();
    [Fact] void should_sort_the_file_table() => _parsed.Files.ShouldEqual(_parsed.Files.Order(StringComparer.Ordinal));
    [Fact] void should_round_trip() => CoverageMapFile.Serialize(_parsed).ShouldEqual(_first);
    [Fact] void should_keep_units_with_no_coverage() => _parsed.Units.Select(unit => unit.Name).ShouldContain("Foo.Specs.Z.when_empty");

    static CoverageMap Build(bool shuffled)
    {
        var units = new List<(string, IEnumerable<string>, IEnumerable<string>)>
        {
            ("Foo.Specs.B.when_b", new[] { "Source/Foo/B.cs", "Source/Foo/A.cs" }, new[] { "Source/Foo.Specs/B.cs" }),
            ("Foo.Specs.A.when_a", new[] { "Source/Foo/A.cs" }, new[] { "Source/Foo.Specs/A.cs" }),
            ("Foo.Specs.Z.when_empty", Array.Empty<string>(), new[] { "Source/Foo.Specs/Z.cs" })
        };
        if (shuffled)
        {
            units.Reverse();
        }

        return CoverageMap.Create("abc123", Maps.TestProject, UnitGranularity.Class, units);
    }
}
