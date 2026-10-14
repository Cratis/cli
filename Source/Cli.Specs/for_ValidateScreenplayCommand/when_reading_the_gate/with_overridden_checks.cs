// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_overridden_checks : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true,\"openQuestions\":\"report\"}}");
    Task Because() => Run("--check", "event-consumers,field-origins");
    [Fact] void should_fail_on_the_replacement_checks() => _exitCode.ShouldEqual(5);
    [Fact] void should_replace_the_file_list() => Gate.GetProperty("checks").GetProperty("value").GetArrayLength().ShouldEqual(2);
    [Fact] void should_not_run_navigation() => _error.ShouldNotContain("PLAY0537");
    [Fact] void should_attribute_checks_to_the_command_line() => SourceOf("checks").ShouldEqual("commandLine");
    [Fact] void should_preserve_file_warnings_as_errors() => SourceOf("warningsAsErrors").ShouldEqual("file");
    [Fact] void should_preserve_file_questions() => SourceOf("openQuestions").ShouldEqual("file");
}
