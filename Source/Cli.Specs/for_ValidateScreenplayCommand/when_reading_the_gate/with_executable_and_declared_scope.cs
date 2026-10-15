// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_executable_and_declared_scope : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"scope\":\"M\"}}");
    Task Because() => Run("--executable");
    [Fact] void should_refuse_input() => _exitCode.ShouldEqual(1);
    [Fact] void should_name_the_declared_gate() => _error.ShouldContain("screenplay.json");
    [Fact] void should_suggest_bypassing_the_gate() => _error.ShouldContain("--no-gate-file");
    [Fact] void should_not_report_a_compilation_result() => _output.ShouldBeEmpty();
}
