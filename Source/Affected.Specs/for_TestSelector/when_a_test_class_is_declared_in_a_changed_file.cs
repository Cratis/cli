// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_a_test_class_is_declared_in_a_changed_file : Specification
{
    ProjectSelection _result;

    void Because()
    {
        var map = Maps.For(
            "base",
            ("Foo.Specs.A.when_a", ["Source/Foo/A.cs"]),
            ("Foo.Specs.B.when_b", ["Source/Foo/B.cs"]));
        _result = Maps.Select(map, Maps.Modified("Source/Foo.Specs/when_b.cs"));
    }

    [Fact] void should_select_that_test() => _result.Entries.Single().Value.ShouldEqual("Foo.Specs.B.when_b");
}
