// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_members_absent_from_model : given.a_structural_comparison
{
    void Establish() => _after += "      screen History\n";
    void Because() => Compare();
    [Fact] void should_have_real_code_only_member_hashes() => _difference.Members.Any(member => member.BeforeHash is null && member.AfterHash is not null && member.Declaration.Kind == "Screen").ShouldBeTrue();
    [Fact] void should_report_every_code_only_screen_member_as_a_blocking_shape_mismatch() => _difference.Members.Where(member => member.Declaration.Kind == "Screen" && member.BeforeHash is null && member.AfterHash is not null).All(member => MemberFinding(member).Category == "ShapeMismatch" && MemberFinding(member).Blocking).ShouldBeTrue();
    [Fact] void should_block() => _exitCode.ShouldEqual(1);
}
