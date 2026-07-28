// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnostics.when_grouping_by_severity;

public class and_severities_are_mixed : Specification
{
    IReadOnlyList<IGrouping<ScreenplayDiagnosticSeverity, ScreenplayDiagnostic>> _result;

    void Because() => _result = ScreenplayDiagnostics.GroupBySeverity(
    [
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "a warning", null),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0200", "an error", null),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0101", "another warning", null)
    ]);

    [Fact] void should_produce_one_group_per_severity() => _result.Count.ShouldEqual(2);
    [Fact] void should_put_the_errors_first() => _result[0].Key.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_put_the_warnings_after_them() => _result[1].Key.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_keep_both_warnings_together() => _result[1].Count().ShouldEqual(2);
}
