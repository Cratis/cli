// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnostics.when_resolving_the_exit_code;

public class and_there_are_only_warnings : Specification
{
    int _result;

    void Because() => _result = ScreenplayDiagnostics.ExitCodeFor(
    [
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "a warning", null),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Information, "SP0300", "a note", null)
    ]);

    [Fact] void should_be_success() => _result.ShouldEqual(ExitCodes.Success);
}
