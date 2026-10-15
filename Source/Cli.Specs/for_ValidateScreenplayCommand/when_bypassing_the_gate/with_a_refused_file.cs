// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_bypassing_the_gate;

public class with_a_refused_file : given.a_validation_gate_cli_process
{
    void Establish() => Declare("{broken");
    Task Because() => Run("--no-gate-file");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_keep_the_ignored_gate_path() => Gate.GetProperty("gateFilePath").GetString().ShouldEqual(_gatePath);
    [Fact] void should_use_defaults() => SourceOf("checks").ShouldEqual("default");
    [Fact] void should_not_report_a_refusal() => _error.ShouldBeEmpty();
}
