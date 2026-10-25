// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_running_install_only : given.a_conformance_run
{
    void Establish()
    {
        _settings.InstallOnly = true;
        _fixture = _fixture with { Checks = [new() { Type = "file_exists", Path = ".cratis/ai/rules/general.md", Phase = AiTestPhase.Install }, new() { Type = "file_exists", Path = "result.txt" }] };
    }

    async Task Because() => _report = await Run();

    [Fact] void should_pass() => _report.Results.Single().Status.ShouldEqual("passed");
    [Fact] void should_never_invoke_harness() => _harness.DidNotReceive().Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AiTestBounds>(), Arg.Any<CancellationToken>());
    [Fact] void should_only_evaluate_install_checks() => _report.Results.Single().Checks.Single().Phase.ShouldEqual(AiTestPhase.Install);
    [Fact] void should_keep_requested_harness_id() => _report.Results.Single().Harness.ShouldEqual("claude");
    [Fact] void should_mark_result_phase_not_run() => _report.Results.Single().ResultPhase.ShouldEqual("not_run");
    [Fact] void should_exit_zero() => _report.ExitCode.ShouldEqual(0);
}
