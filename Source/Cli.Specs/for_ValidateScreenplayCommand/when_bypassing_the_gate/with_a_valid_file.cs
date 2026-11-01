// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_bypassing_the_gate;

public class with_a_valid_file : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true,\"scope\":\"Missing\",\"openQuestions\":\"fail\"}}");
    Task Because() => Run("--no-gate-file", "--check", "navigation");
    [Fact] void should_ignore_the_file_gate() => _exitCode.ShouldEqual(0);
    [Fact] void should_apply_explicit_checks() => SourceOf("checks").ShouldEqual("commandLine");
    [Fact] void should_use_default_warnings_as_errors() => SourceOf("warningsAsErrors").ShouldEqual("default");
    [Fact] void should_use_default_scope() => SourceOf("scope").ShouldEqual("default");
    [Fact] void should_use_default_questions() => SourceOf("openQuestions").ShouldEqual("default");
}
