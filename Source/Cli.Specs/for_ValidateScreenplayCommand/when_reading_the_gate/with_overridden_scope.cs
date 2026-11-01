// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_overridden_scope : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"scope\":\"Missing\",\"checks\":[\"navigation\"],\"warningsAsErrors\":false}}");
    Task Because() => Run("--scope", "M.F.View");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_select_the_command_line_scope() => Summary.GetProperty("scope").GetString().ShouldEqual("M.F.View");
    [Fact] void should_attribute_scope_to_the_command_line() => SourceOf("scope").ShouldEqual("commandLine");
    [Fact] void should_keep_file_checks() => SourceOf("checks").ShouldEqual("file");
    [Fact] void should_keep_file_warnings() => SourceOf("warningsAsErrors").ShouldEqual("file");
}
