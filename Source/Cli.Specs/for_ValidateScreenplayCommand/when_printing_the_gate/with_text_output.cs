// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

public class with_text_output : given.a_validation_gate_cli_process
{
    void Establish()
    {
        _format = "plain";
        Declare("{\"validation\":{\"warningsAsErrors\":true}}");
        File.WriteAllText(_document, "broken model");
    }
    Task Because() => Run("--print-gate", "--check", "navigation");
    [Fact] void should_succeed_without_compiling() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_the_model_root() => _output.ShouldContain($"Model root: {_root}");
    [Fact] void should_print_the_gate_path() => _output.ShouldContain($"Gate file: {_gatePath}");
    [Fact] void should_print_command_line_sources() => _output.ShouldContain("Checks: navigation (source: commandLine)");
    [Fact] void should_print_file_sources() => _output.ShouldContain("Warnings as errors: true (source: file)");
    [Fact] void should_print_default_sources() => _output.ShouldContain("Scope: none (source: default)");
    [Fact] void should_print_unevaluated_open_questions() => _output.ShouldContain("Open questions: not evaluated (source: default)");
    [Fact] void should_not_emit_diagnostics() => _error.ShouldBeEmpty();
}
