// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_UnitNames;

public class when_grouping_units : Specification
{
    string _classUnderSubject;
    string _nestedBelowSubject;
    string _noSubject;
    string _testUnit;

    void Because()
    {
        _classUnderSubject = UnitNames.GroupOf("Cratis.Arc.for_ArcApplication.when_starting", UnitGranularity.Class);
        _nestedBelowSubject = UnitNames.GroupOf("Cratis.Arc.for_ArcApplication.given.when_starting", UnitGranularity.Class);
        _noSubject = UnitNames.GroupOf("Cratis.Arc.Integration.when_starting", UnitGranularity.Class);
        _testUnit = UnitNames.GroupOf("Cratis.Arc.for_ArcApplication.when_starting.should_start", UnitGranularity.Test);
    }

    [Fact] void should_group_a_class_by_its_for_namespace() => _classUnderSubject.ShouldEqual("Cratis.Arc.for_ArcApplication");
    [Fact] void should_cut_nested_namespaces_at_the_for_namespace() => _nestedBelowSubject.ShouldEqual("Cratis.Arc.for_ArcApplication");
    [Fact] void should_use_the_whole_namespace_without_a_for_namespace() => _noSubject.ShouldEqual("Cratis.Arc.Integration");
    [Fact] void should_group_an_individual_test_by_its_class_namespace() => _testUnit.ShouldEqual("Cratis.Arc.for_ArcApplication");
}
