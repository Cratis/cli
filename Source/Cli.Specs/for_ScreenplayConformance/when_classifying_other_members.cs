// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_other_members : given.a_structural_comparison
{
    void Establish() => _after = Source.Replace("name String\n        produces", "name String\n        validate\n          name not empty message \"Required\"\n        produces", StringComparison.Ordinal);
    void Because() => Compare();
    [Fact] void should_have_real_changes_to_existing_command_members() => _difference.Members.Any(member => member.Declaration.Kind == "Command" && member.BeforeHash is not null && member.AfterHash is not null).ShouldBeTrue();
    [Fact] void should_report_every_changed_command_member_as_a_blocking_shape_mismatch() => _difference.Members.Where(member => member.Declaration.Kind == "Command" && member.AfterHash is not null).All(member => MemberFinding(member).Category == "ShapeMismatch" && MemberFinding(member).Blocking).ShouldBeTrue();
    [Fact] void should_block() => _exitCode.ShouldEqual(1);
}
