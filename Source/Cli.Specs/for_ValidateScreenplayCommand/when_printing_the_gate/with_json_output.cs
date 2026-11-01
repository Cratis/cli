// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

public class with_json_output : given.a_validation_gate_cli_process
{
    void Establish()
    {
        Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true}}");
        File.WriteAllText(_document, "broken model");
    }
    Task Because() => Run("--print-gate");
    [Fact] void should_succeed_without_compiling_broken_source() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_the_gate_object() => Summary.GetProperty("modelRoot").GetString().ShouldEqual(_root);
    [Fact] void should_print_canonical_checks() => Summary.GetProperty("checks").GetProperty("value")[0].GetString().ShouldEqual("navigation");
    [Fact] void should_print_the_setting_source() => Summary.GetProperty("warningsAsErrors").GetProperty("source").GetString().ShouldEqual("file");
    [Fact] void should_not_emit_diagnostics() => _error.ShouldBeEmpty();
}
