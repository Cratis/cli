// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_completeness;

public class and_the_model_has_errors : given.a_completeness_cli_process
{
    void Establish() => File.WriteAllText(_document, "module M\n  feature F\n    slice Invalid\n");
    async Task Because() => await Run("--check", "all");

    [Fact] void should_fail_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_skipped_checks_in_the_summary() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("completenessStatus").GetString().ShouldEqual("skipped");
    [Fact] void should_explain_why_checks_were_skipped() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("completenessNote").GetString().ShouldEqual("completeness checks skipped: the model has 1 error(s)");
    [Fact] void should_not_put_the_skip_note_in_diagnostics() => _error.ShouldNotContain("completeness checks skipped");
}
