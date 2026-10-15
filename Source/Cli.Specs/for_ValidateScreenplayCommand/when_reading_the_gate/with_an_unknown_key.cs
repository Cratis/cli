// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_an_unknown_key : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"unknown\":true}}");
    Task Because() => Run();
    [Fact] void should_refuse_input() => _exitCode.ShouldEqual(1);
    [Fact] void should_locate_the_unknown_key() => _error.ShouldContain("screenplay.json:1:");
    [Fact] void should_explain_the_refusal() => _error.ShouldContain("Unknown validation member.");
}
