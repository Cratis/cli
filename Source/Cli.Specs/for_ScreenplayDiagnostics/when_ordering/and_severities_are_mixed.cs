// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnostics.when_ordering;

public class and_severities_are_mixed : Specification
{
    IReadOnlyList<ScreenplayDiagnostic> _result;

    void Because() => _result = ScreenplayDiagnostics.Order(
    [
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Information, "SP0300", "third", null),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0200", "second", "Library.Lending"),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "first", null),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0100", "zeroth", "Library.Authors")
    ]);

    [Fact] void should_put_the_errors_first() => _result[0].Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_order_errors_by_code() => _result[0].Code.ShouldEqual("SP0100");
    [Fact] void should_keep_the_second_error_next() => _result[1].Code.ShouldEqual("SP0200");
    [Fact] void should_put_the_warning_after_the_errors() => _result[2].Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_put_the_information_last() => _result[3].Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
}
