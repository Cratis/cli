// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

public class with_quiet_output : given.a_validation_gate_cli_process
{
    void Establish()
    {
        _format = "quiet";
        File.WriteAllText(_document, "broken model");
    }
    Task Because() => Run("--print-gate");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_nothing() => _output.ShouldBeEmpty();
    [Fact] void should_not_emit_diagnostics() => _error.ShouldBeEmpty();
}
