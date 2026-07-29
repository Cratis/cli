// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnosticsWriter.when_formatting_a_line;

public class and_the_diagnostic_has_a_code : Specification
{
    string _result;

    void Because() => _result = ScreenplayDiagnosticsWriter.LineFor(
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0200", "an error", "Library.Lending"));

    [Fact] void should_write_the_code_before_the_location() => _result.ShouldEqual("  error SP0200: [Library.Lending] an error");
}
