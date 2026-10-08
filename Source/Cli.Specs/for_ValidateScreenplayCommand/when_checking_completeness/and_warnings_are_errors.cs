// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_completeness;

public class and_warnings_are_errors : given.a_completeness_cli_process
{
    async Task Because() => await Run("--check", "navigation", "--warnings-as-errors");

    [Fact] void should_fail_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_warning() => _error.ShouldContain("PLAY0537");
    [Fact] void should_explain_the_warning_count() => _error.ShouldContain("Validation reported 0 error(s) and 1 warning(s)");
    [Fact] void should_keep_the_completeness_summary() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("completenessStatus").GetString().ShouldEqual("ran");
}
