// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_declared_checks_and_warnings_as_errors : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true}}");
    Task Because() => Run();
    [Fact] void should_fail_on_the_declared_completeness_warning() => _exitCode.ShouldEqual(5);
    [Fact] void should_run_the_declared_check() => _error.ShouldContain("PLAY0537");
    [Fact] void should_report_the_failure_summary() => Summary.GetProperty("valid").GetBoolean().ShouldBeFalse();
    [Fact] void should_attribute_checks_to_the_file() => SourceOf("checks").ShouldEqual("file");
    [Fact] void should_keep_file_warnings_as_errors_without_a_flag() => SourceOf("warningsAsErrors").ShouldEqual("file");
    [Fact] void should_keep_top_level_checks() => Summary.GetProperty("checks")[0].GetString().ShouldEqual("navigation");
}
