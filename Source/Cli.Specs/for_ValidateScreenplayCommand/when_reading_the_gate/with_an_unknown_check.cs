// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_an_unknown_check : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"checks\":[\"unknown\"]}}");
    Task Because() => Run();
    [Fact] void should_refuse_input_instead_of_a_cli_selection_error() => _exitCode.ShouldEqual(1);
    [Fact] void should_locate_the_unknown_check() => _error.ShouldContain("screenplay.json:1:");
    [Fact] void should_name_the_unknown_check() => _error.ShouldContain("unknown");
}
