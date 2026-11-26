// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_CoverageAttributor;

public class when_grouping_units : Specification
{
    static readonly string[] _units =
    [
        .. Enumerable.Range(0, 10).Select(index => $"Cratis.A.for_Thing{index:00}.when_a"),
        .. Enumerable.Range(0, 10).Select(index => $"Cratis.A.for_Thing{index:00}.when_b")
    ];

    IReadOnlyList<IReadOnlyList<string>> _perNamespace;
    IReadOnlyList<IReadOnlyList<string>> _bounded;
    IReadOnlyList<IReadOnlyList<string>> _perUnit;

    void Because()
    {
        _perNamespace = CoverageAttributor.GroupUnits(_units, UnitGranularity.Class, AttributionGroup.Namespace, 0);
        _bounded = CoverageAttributor.GroupUnits(_units, UnitGranularity.Class, AttributionGroup.Namespace, 3);
        _perUnit = CoverageAttributor.GroupUnits(_units, UnitGranularity.Class, AttributionGroup.Unit, 3);
    }

    [Fact] void should_run_one_group_per_namespace_without_a_limit() => _perNamespace.Count.ShouldEqual(10);
    [Fact] void should_share_runs_between_neighbouring_namespaces_beyond_the_limit() => _bounded.Count.ShouldEqual(3);
    [Fact] void should_keep_every_unit_in_exactly_one_group() => _bounded.SelectMany(group => group).Order().ShouldEqual(_units.Order());
    [Fact] void should_keep_the_namespaces_of_a_unit_together() => _bounded.All(group => group.Count % 2 == 0).ShouldBeTrue();
    [Fact] void should_not_bound_units_that_run_alone() => _perUnit.Count.ShouldEqual(_units.Length);
}
