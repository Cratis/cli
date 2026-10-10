// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_application_members : given.a_structural_comparison
{
    void Establish() => _after = Source.Replace("domain Library", "domain Other", StringComparison.Ordinal);
    void Because() => Compare();
    [Fact] void should_have_real_application_member_changes() => _difference.Members.Any(member => member.Declaration.Kind == "Application").ShouldBeTrue();
    [Fact] void should_report_them_as_informational() => _findings.Where(finding => finding.Kind == "Application").All(finding => finding.Category == "Informational" && !finding.Blocking).ShouldBeTrue();
    [Fact] void should_not_block() => _exitCode.ShouldEqual(0);
}
