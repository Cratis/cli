// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_install_only_reports_bounds : given.a_conformance_run
{
    void Establish()
    {
        _settings.InstallOnly = true;
        _fixture = _fixture with
        {
            Bounds = new() { TimeoutSeconds = 20, MaxBudgetUsd = 1, MaxTurns = 2 },
            Checks = [new() { Type = "file_exists", Path = ".cratis/ai/rules/general.md", Phase = AiTestPhase.Install }, new() { Type = "file_exists", Path = "result.txt" }]
        };
    }

    async Task Because() => _report = await Run();

    [Fact] void should_enforce_timeout() => _report.Results.Single().Bounds!.Timeout.Enforcement.ShouldEqual("enforced");
    [Fact] void should_not_apply_budget() => _report.Results.Single().Bounds!.Budget.Enforcement.ShouldEqual("not_applicable");
    [Fact] void should_not_apply_turns() => _report.Results.Single().Bounds!.Turns.Enforcement.ShouldEqual("not_applicable");
}
