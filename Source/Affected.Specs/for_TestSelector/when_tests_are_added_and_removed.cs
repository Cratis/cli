// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_tests_are_added_and_removed : Specification
{
    ProjectSelection _newClass;
    ProjectSelection _removed;
    ProjectSelection _newMethod;

    void Because()
    {
        var classMap = Maps.For(
            "base",
            ("Foo.Specs.A.when_a", ["Source/Foo/A.cs"]),
            ("Foo.Specs.B.when_b", ["Source/Foo/B.cs"]),
            ("Foo.Specs.C.when_c", ["Source/Foo/C.cs"]));
        _newClass = Maps.Select(classMap, [], discovered: ["Foo.Specs.A.when_a", "Foo.Specs.B.when_b", "Foo.Specs.C.when_c", "Foo.Specs.N.when_new"]);
        _removed = Maps.Select(classMap, [], discovered: ["Foo.Specs.A.when_a", "Foo.Specs.B.when_b"]);

        var testMap = CoverageMap.Create(
            "base",
            Maps.TestProject,
            UnitGranularity.Test,
            [
                ("Foo.Specs.A.when_a.should_one", ["Source/Foo/A.cs"], ["Source/Foo.Specs/A.cs"]),
                ("Foo.Specs.A.when_a.should_two", ["Source/Foo/A.cs"], ["Source/Foo.Specs/A.cs"]),
                ("Foo.Specs.B.when_b.should_one", ["Source/Foo/B.cs"], ["Source/Foo.Specs/B.cs"])
            ]);
        _newMethod = Maps.Select(testMap, [], discovered: ["Foo.Specs.A.when_a.should_one", "Foo.Specs.A.when_a.should_two", "Foo.Specs.A.when_a.should_three", "Foo.Specs.B.when_b.should_one"]);
    }

    [Fact] void should_select_a_new_test_class() => _newClass.Entries.Single().Value.ShouldEqual("Foo.Specs.N.when_new");
    [Fact] void should_say_it_is_new() => _newClass.Reasons.Single().ShouldContain("new test");
    [Fact] void should_ignore_a_removed_test() => _removed.Mode.ShouldEqual(SelectionMode.Skip);
    [Fact] void should_select_a_new_test_method() => _newMethod.Entries.Single().Value.ShouldEqual("Foo.Specs.A.when_a.should_three");
}
