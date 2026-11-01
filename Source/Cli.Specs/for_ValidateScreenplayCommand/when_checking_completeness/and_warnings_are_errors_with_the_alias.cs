// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_completeness;

public class and_warnings_are_errors_with_the_alias : given.a_completeness_cli_process
{
    int _canonicalExitCode;
    string _canonicalOutput;
    string _canonicalError;

    async Task Because()
    {
        await Run("--check", "navigation", "--warnings-as-errors");
        _canonicalExitCode = _exitCode;
        _canonicalOutput = _output;
        _canonicalError = _error;
        await Run("--check", "navigation", "--warnaserror");
    }

    [Fact] void should_fail_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_match_the_canonical_exit_code() => _exitCode.ShouldEqual(_canonicalExitCode);
    [Fact] void should_match_the_canonical_summary() => _output.ShouldEqual(_canonicalOutput);
    [Fact] void should_match_the_canonical_diagnostics() => _error.ShouldEqual(_canonicalError);
}
