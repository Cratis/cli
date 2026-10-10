// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_added_declarations : given.a_structural_comparison
{
    void Establish() => _after += "      event Extra\n        name String\n";
    void Because() => Compare();
    [Fact] void should_mark_code_only_declarations_missing_from_model() => _findings.Any(finding => finding.Category == "MissingFromModel" && finding.Kind == "Event" && finding.Blocking).ShouldBeTrue();
    [Fact] void should_block() => _exitCode.ShouldEqual(1);
}
