// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_harness_is_unavailable_after_install : given.a_conformance_run
{
    void Establish()
    {
        _fixture = _fixture with { Checks = [new() { Type = "file_exists", Path = ".cratis/ai/rules/general.md", Phase = AiTestPhase.Install }, new() { Type = "file_exists", Path = "result.txt" }] };
        _harness.Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AiTestBounds>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AiTestHarnessResult("skipped", "Executable unavailable")));
    }

    async Task Because() => _report = await Run();

    [Fact] void should_be_skipped() => _report.Results.Single().Status.ShouldEqual("skipped");
    [Fact] void should_keep_install_check_results() => _report.Results.Single().Checks.Single().Status.ShouldEqual("passed");
    [Fact] void should_not_mark_result_phase() => _report.Results.Single().ResultPhase.ShouldBeNull();
}
