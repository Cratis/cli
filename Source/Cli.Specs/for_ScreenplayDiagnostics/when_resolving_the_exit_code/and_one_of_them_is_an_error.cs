// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnostics.when_resolving_the_exit_code;

public class and_one_of_them_is_an_error : Specification
{
    int _result;

    void Because() => _result = ScreenplayDiagnostics.ExitCodeFor(
    [
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "a warning", null),
        new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0200", "an error", null)
    ]);

    [Fact] void should_be_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_be_success() => _result.ShouldNotEqual(ExitCodes.Success);
}
