// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_event_contract_changes : given.a_structural_comparison
{
    void Establish() => _after += "        extra String\n";
    void Because() => Compare();
    [Fact] void should_classify_all_contract_changes_as_blocking_shape_mismatches() => _findings.Where(finding => finding.Change == "PropertyAdded").All(finding => finding.Category == "ShapeMismatch" && finding.Blocking).ShouldBeTrue();
    [Fact] void should_retain_event_property_changes() => _difference.Events.ShouldNotBeEmpty();
    [Fact] void should_block() => _exitCode.ShouldEqual(1);
}
