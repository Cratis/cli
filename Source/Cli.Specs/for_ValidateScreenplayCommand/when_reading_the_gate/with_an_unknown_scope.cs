// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_an_unknown_scope : given.a_validation_gate_cli_process
{
    JsonElement Error => JsonSerializer.Deserialize<JsonElement>(_error);

    void Establish() => Declare("{\"validation\":{\"scope\":\"Unknown\"}}");
    Task Because() => Run();
    [Fact] void should_refuse_input() => _exitCode.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_explain_the_unknown_scope() => Error.GetProperty("message").GetString().ShouldContain("Unknown scope 'Unknown'");
    [Fact] void should_suggest_correcting_the_gate_file() => Error.GetProperty("suggestion").GetString().ShouldContain("screenplay.json");
    [Fact] void should_suggest_bypassing_the_gate() => Error.GetProperty("suggestion").GetString().ShouldContain("--no-gate-file");
    [Fact] void should_not_report_a_compilation_result() => _output.ShouldBeEmpty();
}
