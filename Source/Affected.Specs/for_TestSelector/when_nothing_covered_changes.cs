// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_nothing_covered_changes : Specification
{
    ProjectSelection _result;

    void Because()
    {
        var map = Maps.For("base", ("Foo.Specs.A.when_a", ["Source/Foo/A.cs"]));
        _result = Maps.Select(map, Maps.Modified(".cratis/affected/maps/other.json"), discovered: ["Foo.Specs.A.when_a"]);
    }

    [Fact] void should_skip_the_project() => _result.Mode.ShouldEqual(SelectionMode.Skip);
}
