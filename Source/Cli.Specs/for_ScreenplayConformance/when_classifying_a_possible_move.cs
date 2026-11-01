// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_a_possible_move : given.a_structural_comparison
{
    void Establish() => _after = Source.Replace("feature Registration", "feature Other", StringComparison.Ordinal);
    void Because() => Compare();
    [Fact] void should_hint_same_name_counterparts() => _findings.Any(finding => finding.Kind == "Command" && finding.SameNameCounterpart is not null).ShouldBeTrue();
    [Fact] void should_still_block_added_declarations() => _exitCode.ShouldEqual(1);
    [Fact] void should_not_infer_renames() => _findings.Any(finding => finding.Change == "Renamed").ShouldBeFalse();
}
