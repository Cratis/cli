// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

public class with_executable_and_declared_scope : given.a_validation_gate_cli_process
{
    void Establish()
    {
        Declare("{\"validation\":{\"scope\":\"Unknown\"}}");
        File.WriteAllText(_document, "broken model");
    }
    Task Because() => Run("--print-gate", "--executable");
    [Fact] void should_succeed_without_compiling() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_print_the_declared_scope() => Summary.GetProperty("scope").GetProperty("value").GetString().ShouldEqual("Unknown");
    [Fact] void should_attribute_scope_to_the_file() => Summary.GetProperty("scope").GetProperty("source").GetString().ShouldEqual("file");
    [Fact] void should_not_emit_a_refusal() => _error.ShouldBeEmpty();
}
