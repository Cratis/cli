// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_an_unknown_command_line_check : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"checks\":[\"navigation\"]}}");
    Task Because() => Run("--check", "unknown");
    [Fact] void should_fail_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_unknown_selection() => JsonSerializer.Deserialize<JsonElement>(_error).GetProperty("message").GetString().ShouldContain("Unknown --check selection 'unknown'");
    [Fact] void should_report_a_validation_error() => _error.ShouldContain(ExitCodes.ValidationErrorCode);
    [Fact] void should_not_compile_the_model() => _output.ShouldBeEmpty();
}
