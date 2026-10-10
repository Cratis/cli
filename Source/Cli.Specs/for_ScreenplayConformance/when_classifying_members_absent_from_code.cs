// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_members_absent_from_code : given.a_structural_comparison
{
    void Establish() => _before += "      screen History\n";
    void Because() => Compare();
    [Fact] void should_have_real_absent_member_hashes() => _difference.Members.Any(member => member.BeforeHash is not null && member.AfterHash is null).ShouldBeTrue();
    [Fact] void should_report_nonblocking_shape_mismatches() => _findings.Any(finding => finding.Category == "ShapeMismatch" && !finding.Blocking).ShouldBeTrue();
    [Fact] void should_not_block() => _exitCode.ShouldEqual(0);
}
