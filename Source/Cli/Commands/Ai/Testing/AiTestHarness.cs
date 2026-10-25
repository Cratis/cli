// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Cli.Commands.Ai.Testing;

[Flags]
internal enum AiTestHarnessCapabilities
{
    None = 0,
    ObservesInstructionLoads = 1,
    BudgetLimit = 2,
    TurnLimit = 4
}

internal interface IAiTestHarness
{
    string Id { get; }
    AiTestHarnessCapabilities Capabilities { get; }
    Task<AiTestHarnessResult> Run(string workspace, string prompt, AiTestBounds bounds, CancellationToken cancellationToken);
}

internal sealed record AiTestHarnessResult(string Status, string? Reason = null, string? Version = null, string Transcript = "");

internal sealed class AiTestHarness(string id, IAiTestProcess process) : IAiTestHarness
{
    static readonly string[] _credentialErrors = ["not logged in", "authentication required", "invalid api key", "missing api key", "please login", "please log in"];

    public string Id => id;
    public AiTestHarnessCapabilities Capabilities => id == "claude"
        ? AiTestHarnessCapabilities.ObservesInstructionLoads | AiTestHarnessCapabilities.BudgetLimit | AiTestHarnessCapabilities.TurnLimit
        : AiTestHarnessCapabilities.None;

    public async Task<AiTestHarnessResult> Run(string workspace, string prompt, AiTestBounds bounds, CancellationToken cancellationToken)
    {
        var unsupported = Unsupported();
        if (unsupported is not null) return new("skipped", unsupported);
        var version = await process.Run(new(id, ["--version"], workspace), TimeSpan.FromSeconds(Math.Min(10, bounds.TimeoutSeconds)), cancellationToken);
        if (version.Status != "completed") return new(version.Status, version.Reason);
        var result = await process.Run(new(id, Arguments(workspace, prompt, bounds), workspace), TimeSpan.FromSeconds(bounds.TimeoutSeconds), cancellationToken);
        var status = Status(result);
        return new(status, result.Reason ?? (status == "passed" ? null : "Harness did not complete; credentials, CLI mode or execution may be unavailable."), version.Output.Trim(), result.Output);
    }

    internal IReadOnlyList<string> Arguments(string workspace, string prompt, AiTestBounds bounds)
    {
        if (id == "codex") return ["exec", "--json", "--ephemeral", "--ignore-user-config", "--sandbox", "workspace-write", "--skip-git-repo-check", "-C", workspace, prompt];
        var arguments = new List<string> { "-p", prompt, "--output-format", "stream-json", "--verbose", "--setting-sources", "project", "--no-session-persistence", "--permission-mode", "acceptEdits" };
        if (bounds.MaxBudgetUsd is not null) arguments.AddRange(["--max-budget-usd", bounds.MaxBudgetUsd.Value.ToString(CultureInfo.InvariantCulture)]);
        if (bounds.MaxTurns is not null) arguments.AddRange(["--max-turns", bounds.MaxTurns.Value.ToString(CultureInfo.InvariantCulture)]);
        return arguments;
    }

    static string Status(AiTestProcessResult result)
    {
        if (result.ExitCode != 0 && MissingCredentials(result.Output)) return "skipped";
        if (result.Status == "completed") return result.ExitCode == 0 ? "passed" : "error";
        return result.Status;
    }

    static bool MissingCredentials(string output) => _credentialErrors.Any(message => output.Contains(message, StringComparison.OrdinalIgnoreCase));

    string? Unsupported() => id is not ("claude" or "codex") ? $"Unsupported harness: {id}" : null;
}
