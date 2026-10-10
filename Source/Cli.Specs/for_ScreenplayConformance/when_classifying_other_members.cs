// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_other_members : given.a_structural_comparison
{
    void Establish() => _after = Source.Replace("name String\n        produces", "name String\n        validate\n          name not empty message \"Required\"\n        produces", StringComparison.Ordinal);
    void Because() => Compare();
    [Fact] void should_have_real_member_changes() => _difference.Members.ShouldNotBeEmpty();
    [Fact] void should_report_blocking_shape_mismatches() => _findings.Any(finding => finding.Category == "ShapeMismatch" && finding.Kind == "Command" && finding.Blocking).ShouldBeTrue();
    [Fact] void should_block() => _exitCode.ShouldEqual(1);
}
