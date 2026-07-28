// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnosticsWriter.when_formatting_a_line;

public class and_the_diagnostic_has_no_code : Specification
{
    string _result;

    void Because() => _result = ScreenplayDiagnosticsWriter.LineFor(
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, string.Empty, "a warning", "MyApp.play(3,1)"));

    [Fact] void should_leave_the_code_out() => _result.ShouldEqual("  warning: [MyApp.play(3,1)] a warning");
}
