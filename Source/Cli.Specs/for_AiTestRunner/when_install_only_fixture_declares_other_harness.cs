// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_install_only_fixture_declares_other_harness : given.a_conformance_run
{
    void Establish()
    {
        _settings.InstallOnly = true;
        _fixture = _fixture with { Harnesses = ["codex"], Checks = [new() { Type = "file_exists", Path = ".cratis/ai/rules/general.md", Phase = AiTestPhase.Install }, new() { Type = "file_exists", Path = "result.txt" }] };
    }

    async Task Because() => _report = await Run();

    [Fact] void should_produce_one_result() => _report.Results.Count.ShouldEqual(1);
    [Fact] void should_use_the_declared_harness() => _report.Results.Single().Harness.ShouldEqual("codex");
    [Fact] void should_pass() => _report.Results.Single().Status.ShouldEqual("passed");
}
