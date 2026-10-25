// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_reporting_skips_and_bounds : given.a_conformance_run
{
    void Establish()
    {
        _settings.Harnesses = "codex,unsupported";
        _settings.Timeout = 300;
        _settings.MaxBudgetUsd = 10;
        _fixture = _fixture with { Bounds = new() { TimeoutSeconds = 20, MaxBudgetUsd = 1, MaxTurns = 2 } };
        _process.Run(Arg.Any<AiTestProcessRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AiTestProcessResult("skipped", null, "", "Executable unavailable")));
        _runner = new([new AiTestHarness("codex", _process)], _process, new([]));
    }
    async Task Because() => _report = await Run();
    [Fact] void should_skip_missing_executable() => _report.Results[0].Status.ShouldEqual("skipped");
    [Fact] void should_explain_skip() => _report.Results[0].Reason.ShouldContain("Executable unavailable");
    [Fact] void should_skip_unsupported_harness() => _report.Results[1].Status.ShouldEqual("skipped");
    [Fact] void should_not_claim_codex_budget_enforcement() => _report.Results[0].Bounds!.Budget.Enforcement.ShouldEqual("not_enforced");
    [Fact] void should_not_claim_codex_turn_enforcement() => _report.Results[0].Bounds!.Turns.Enforcement.ShouldEqual("not_enforced");
    [Fact] void should_report_timeout_enforcement() => _report.Results[0].Bounds!.Timeout.Enforcement.ShouldEqual("enforced");
    [Fact] void should_not_raise_fixture_timeout() => _report.Results[0].Bounds!.Timeout.Value.ShouldEqual(20);
    [Fact] void should_not_raise_fixture_budget() => _report.Results[0].Bounds!.Budget.Value.ShouldEqual(1);
    [Fact] void should_exit_zero_for_only_skips() => _report.ExitCode.ShouldEqual(0);
    [Fact] void should_warn_that_nothing_passed() => _report.Warning.ShouldContain("no fixtures ran");
}
