// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_removed_declarations : given.a_structural_comparison
{
    void Establish() => _before += "      screen History\n";
    void Because() => Compare();
    [Fact] void should_mark_model_only_declarations_not_realized() => _findings.Any(finding => finding.Category == "NotRealizedInCode" && finding.Kind == "Screen" && !finding.Blocking).ShouldBeTrue();
    [Fact] void should_not_block() => _exitCode.ShouldEqual(0);
}
