// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestHarness;

public class when_adapting_harnesses : Specification
{
    AiTestHarnessResult _claude = null!;
    AiTestHarnessResult _codex = null!;
    AiTestHarnessResult _credentials = null!;
    AiTestHarnessResult _timeout = null!;
    AiTestBoundEnforcement _enforcement = null!;
    IReadOnlyList<string> _claudeArguments = [];
    IReadOnlyList<string> _codexArguments = [];
    IReadOnlyList<AiTestObservedLoad> _observed = [];
    string _redacted = string.Empty;

    async Task Because()
    {
        var process = Substitute.For<IAiTestProcess>();
        process.Run(Arg.Any<AiTestProcessRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AiTestProcessResult("completed", 0, "fake 1")));
        var claude = new AiTestHarness("claude", process);
        var codex = new AiTestHarness("codex", process);
        var bounds = new AiTestBounds { MaxBudgetUsd = 1, MaxTurns = 3 };
        _claudeArguments = claude.Arguments("/workspace", "prompt", bounds);
        _codexArguments = codex.Arguments("/workspace", "prompt", bounds);
        _enforcement = AiTestBoundEnforcement.For(bounds, claude.Capabilities);
        _claude = await claude.Run("/workspace", "prompt", bounds, CancellationToken.None);
        _codex = await codex.Run("/workspace", "prompt", bounds, CancellationToken.None);
        process.Run(Arg.Is<AiTestProcessRequest>(request => request.Arguments.Count > 1), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AiTestProcessResult("completed", 1, "not logged in")));
        _credentials = await claude.Run("/workspace", "prompt", bounds, CancellationToken.None);
        process.Run(Arg.Is<AiTestProcessRequest>(request => request.Arguments.Count > 1), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AiTestProcessResult("error", null, "", "Timeout exceeded")));
        _timeout = await claude.Run("/workspace", "prompt", bounds, CancellationToken.None);
        _observed = AiTestObservedLoads.Read("not json\n{\"message\":{\"content\":[{\"type\":\"tool_use\",\"name\":\"Read\",\"input\":{\"file_path\":\"rules.md\"}}]}}", "/workspace", ["rules.md", "unread.md"], claude.Capabilities);
        _redacted = new AiTestRedactor(["environment-value"]).Redact("environment-value sk-private ghp_private Bearer private TOKEN=private");
    }
    [Fact] void should_complete_claude_with_fake_process() => _claude.Status.ShouldEqual("passed");
    [Fact] void should_complete_codex_despite_unenforced_budget() => _codex.Status.ShouldEqual("passed");
    [Fact] void should_capture_version() => _claude.Version.ShouldEqual("fake 1");
    [Fact] void should_limit_claude_budget() => _claudeArguments.Contains("--max-budget-usd", StringComparer.Ordinal).ShouldBeTrue();
    [Fact] void should_limit_claude_turns() => _claudeArguments.Contains("--max-turns", StringComparer.Ordinal).ShouldBeTrue();
    [Fact] void should_not_disable_instruction_discovery() => _claudeArguments.Contains("--bare", StringComparer.Ordinal).ShouldBeFalse();
    [Fact] void should_request_codex_sandbox() => _codexArguments.Contains("workspace-write", StringComparer.Ordinal).ShouldBeTrue();
    [Fact] void should_not_send_unsupported_budget_option() => _codexArguments.Contains("--max-budget-usd", StringComparer.Ordinal).ShouldBeFalse();
    [Fact] void should_report_claude_budget_enforcement() => _enforcement.Budget.Enforcement.ShouldEqual("enforced");
    [Fact] void should_skip_missing_credentials() => _credentials.Status.ShouldEqual("skipped");
    [Fact] void should_propagate_timeout_error() => _timeout.Status.ShouldEqual("error");
    [Fact] void should_explain_timeout() => _timeout.Reason.ShouldEqual("Timeout exceeded");
    [Fact] void should_report_only_explicit_tool_reads_as_observed() => _observed[0].Status.ShouldEqual("observed");
    [Fact] void should_report_other_paths_as_unknown() => _observed[1].Status.ShouldEqual("unknown");
    [Fact] void should_redact_environment_values() => _redacted.ShouldNotContain("environment-value");
    [Fact] void should_redact_token_formats() => _redacted.ShouldNotContain("private");
}
