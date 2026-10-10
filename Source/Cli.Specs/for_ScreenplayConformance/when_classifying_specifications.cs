// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_specifications : given.a_structural_comparison
{
    void Establish() => _after += "      specification Registers\n        when Register\n          id = \"11111111-1111-1111-1111-111111111111\"\n          name = \"First\"\n        then Registered\n          name = \"First\"\n";
    void Because() => Compare();
    [Fact] void should_have_real_specification_changes() => _difference.Specifications.ShouldNotBeEmpty();
    [Fact] void should_report_specification_changes_as_informational() => _findings.Where(finding => finding.Kind == "Specification").All(finding => finding.Category == "Informational" && !finding.Blocking).ShouldBeTrue();
    [Fact] void should_not_block() => _exitCode.ShouldEqual(0);
}
