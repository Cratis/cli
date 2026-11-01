// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_install_only_check_fails : given.a_conformance_run
{
    void Establish()
    {
        _settings.InstallOnly = true;
        _fixture = _fixture with { Checks = [new() { Type = "file_exists", Path = "missing", Phase = AiTestPhase.Install }, new() { Type = "file_exists", Path = "result.txt" }] };
    }

    async Task Because() => _report = await Run();

    [Fact] void should_fail() => _report.Results.Single().Status.ShouldEqual("failed");
    [Fact] void should_exit_one() => _report.ExitCode.ShouldEqual(1);
    [Fact] void should_never_invoke_harness() => _harness.DidNotReceive().Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AiTestBounds>(), Arg.Any<CancellationToken>());
}
