// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnosticsWriter.when_formatting_a_line;

public class and_the_diagnostic_has_no_location : Specification
{
    string _result;

    void Because() => _result = ScreenplayDiagnosticsWriter.LineFor(
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Information, "SP0001", "something to know", null));

    [Fact] void should_leave_the_location_out() => _result.ShouldEqual("  info SP0001: something to know");
}
