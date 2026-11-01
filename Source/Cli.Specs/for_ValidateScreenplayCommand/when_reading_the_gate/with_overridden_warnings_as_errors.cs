// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_overridden_warnings_as_errors : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":false}}");
    Task Because() => Run("--warnaserror");
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(5);
    [Fact] void should_override_only_warnings_as_errors() => SourceOf("warningsAsErrors").ShouldEqual("commandLine");
    [Fact] void should_keep_declared_checks() => SourceOf("checks").ShouldEqual("file");
}
