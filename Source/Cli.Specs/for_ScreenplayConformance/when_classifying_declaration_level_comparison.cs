// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_declaration_level_comparison : given.a_structural_comparison
{
    void Establish() => _before = _after = Source.Replace("produces Registered", "produces Unknown", StringComparison.Ordinal);
    void Because() => Compare();
    [Fact] void should_report_expected_authoring_coverage() => _difference.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.DeclarationLevelOnly).ShouldBeTrue();
    [Fact] void should_not_fail_on_expected_coverage_limits() => _exitCode.ShouldEqual(0);
}
