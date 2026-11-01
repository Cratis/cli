// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_compiler_errors_and_no_selected_checks : given.a_validation_gate_cli_process
{
    void Establish()
    {
        Declare("{\"validation\":{\"warningsAsErrors\":true}}");
        File.WriteAllText(_document, "broken model");
    }
    Task Because() => Run();
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(5);
    [Fact] void should_report_the_effective_gate_on_failure() => SourceOf("warningsAsErrors").ShouldEqual("file");
    [Fact] void should_mark_the_result_invalid() => Summary.GetProperty("valid").GetBoolean().ShouldBeFalse();
}
