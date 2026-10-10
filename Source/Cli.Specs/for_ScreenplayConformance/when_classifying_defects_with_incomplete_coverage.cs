// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_defects_with_incomplete_coverage : given.a_structural_comparison
{
    void Establish() => _after += "    slice\n";
    void Because() => Compare();
    [Fact] void should_have_incomplete_source_coverage() => _difference.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.IncompleteSource).ShouldBeTrue();
    [Fact] void should_have_a_known_added_declaration() => _findings.Any(finding => finding.Blocking).ShouldBeTrue();
    [Fact] void should_prioritize_known_defects() => _exitCode.ShouldEqual(1);
}
