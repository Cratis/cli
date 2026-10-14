// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

public class with_a_refused_file : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{broken");
    Task Because() => Run("--print-gate");
    [Fact] void should_refuse_input() => _exitCode.ShouldEqual(1);
    [Fact] void should_not_print_a_partial_gate() => _output.ShouldBeEmpty();
    [Fact] void should_locate_the_refusal() => _error.ShouldContain("screenplay.json:1:");
}
