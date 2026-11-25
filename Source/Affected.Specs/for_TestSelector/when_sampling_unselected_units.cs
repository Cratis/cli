// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_sampling_unselected_units : Specification
{
    ProjectSelection _first;
    ProjectSelection _second;
    ProjectSelection _none;

    void Because()
    {
        var units = Enumerable.Range(0, 200).Select(index => ($"Foo.Specs.N{index}.when_x", new[] { $"Source/Foo/F{index}.cs" })).ToArray();
        var map = Maps.For("base", units);
        var sampled = new AffectedConfiguration { SampleRate = 0.1, RollupMinUnits = 1000 };
        _first = Maps.Select(map, Maps.Modified("Source/Foo/F0.cs"), sampled);
        _second = Maps.Select(map, Maps.Modified("Source/Foo/F0.cs"), sampled);
        _none = Maps.Select(map, Maps.Modified("Source/Foo/F0.cs"), new AffectedConfiguration { RollupMinUnits = 1000 });
    }

    [Fact] void should_add_a_canary_sample() => _first.SelectedUnits.ShouldBeGreaterThan(1);
    [Fact] void should_keep_the_sample_small() => _first.SelectedUnits.ShouldBeLessThan(60);
    [Fact] void should_pick_the_same_sample_every_time() => _first.Entries.Select(entry => entry.Value).ShouldEqual(_second.Entries.Select(entry => entry.Value));
    [Fact] void should_not_sample_by_default() => _none.SelectedUnits.ShouldEqual(1);
}
