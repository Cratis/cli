// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_install_checks_fail : given.a_conformance_run
{
    void Establish() => _fixture = _fixture with { Checks = [new() { Type = "file_exists", Path = "missing", Phase = AiTestPhase.Install }, new() { Type = "file_exists", Path = "result.txt" }] };
    async Task Because() => _report = await Run();
    [Fact] void should_fail_before_running_harness() => _workspace.ShouldBeEmpty();
    [Fact] void should_return_failure() => _report.ExitCode.ShouldEqual(1);
    [Fact] void should_report_phase() => _report.Results.Single().Checks.Single().Phase.ShouldEqual(AiTestPhase.Install);
}
