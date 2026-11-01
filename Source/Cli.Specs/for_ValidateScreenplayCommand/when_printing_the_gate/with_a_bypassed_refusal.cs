// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

public class with_a_bypassed_refusal : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{broken");
    Task Because() => Run("--print-gate", "--no-gate-file");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_default_sources() => Summary.GetProperty("checks").GetProperty("source").GetString().ShouldEqual("default");
    [Fact] void should_not_emit_a_refusal() => _error.ShouldBeEmpty();
}
