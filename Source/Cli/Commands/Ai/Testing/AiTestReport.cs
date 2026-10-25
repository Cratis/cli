// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed record AiTestInstalled(string Path, string Hash);
internal sealed record AiTestObservedLoad(string Path, string Status);
internal sealed record AiTestResult(
    string Fixture,
    string Harness,
    string Status,
    string? Reason,
    string? Version,
    string CorpusRevision,
    IReadOnlyList<AiTestInstalled> Installed,
    IReadOnlyList<AiTestObservedLoad> ObservedLoad,
    IReadOnlyList<AiTestCheckResult> Checks,
    string? TranscriptPath = null,
    string UserMemoryIsolation = "unknown",
    AiTestBoundEnforcement? Bounds = null,
    string? ErrorKind = null);

internal sealed record AiTestBound(double? Value, string Enforcement);
internal sealed record AiTestBoundEnforcement(AiTestBound Timeout, AiTestBound Budget, AiTestBound Turns)
{
    internal static AiTestBoundEnforcement For(AiTestBounds bounds, AiTestHarnessCapabilities capabilities) => new(
        new(bounds.TimeoutSeconds, "enforced"),
        Bound((double?)bounds.MaxBudgetUsd, capabilities.HasFlag(AiTestHarnessCapabilities.BudgetLimit)),
        Bound(bounds.MaxTurns, capabilities.HasFlag(AiTestHarnessCapabilities.TurnLimit)));

    static AiTestBound Bound(double? value, bool capable)
    {
        if (value is null) return new(null, "not_requested");
        return new(value, capable ? "enforced" : "not_enforced");
    }
}

internal sealed record AiTestReport(IReadOnlyList<AiTestResult> Results, string? Warning = null, string? Error = null)
{
    internal int ExitCode
    {
        get
        {
            if (Error is not null || Results.Any(result => result.ErrorKind == "installation")) return 2;
            return Results.Any(result => result.Status == "failed" || result.Status == "error") ? 1 : 0;
        }
    }

    internal static AiTestReport Create(IReadOnlyList<AiTestResult> results) => new(results, results.Any(result => result.Status == "passed") ? null : "no fixtures ran successfully; no passed results (skips are not passes)");

    internal int Write(string format)
    {
        if (format == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(this, AiTestFixture.Json));
        }
        else
        {
            foreach (var result in Results) Console.WriteLine($"{result.Fixture} / {result.Harness}: {result.Status} {result.Reason}");
            if (Warning is not null) Console.WriteLine($"Warning: {Warning}");
            if (Error is not null) Console.WriteLine($"Error: {Error}");
            Console.WriteLine($"{Results.Count(result => result.Status == "passed")} passed; installation and behavioral checks do not prove instruction loading.");
        }
        return ExitCode;
    }
}

internal static class AiTestObservedLoads
{
    internal static IReadOnlyList<AiTestObservedLoad> Read(string transcript, string workspace, IEnumerable<string> expected, AiTestHarnessCapabilities capabilities)
    {
        var reads = new HashSet<string>(StringComparer.Ordinal);
        if (capabilities.HasFlag(AiTestHarnessCapabilities.ObservesInstructionLoads))
        {
            foreach (var line in transcript.Split('\n', StringSplitOptions.RemoveEmptyEntries)) ReadLine(line, workspace, reads);
        }
        return [.. expected.Select(path => new AiTestObservedLoad(path, reads.Contains(Path.GetFullPath(Path.Combine(workspace, path))) ? "observed" : "unknown"))];
    }

    static void ReadLine(string line, string workspace, HashSet<string> reads)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<StreamEvent>(line, AiTestFixture.Json);
            if (parsed?.Message?.Content is not { } content) return;
            foreach (var item in content.Where(item => item.Type == "tool_use" && item.Name == "Read"))
            {
                var path = item.Input?.FilePath;
                if (path is not null) reads.Add(Path.GetFullPath(Path.Combine(workspace, path)));
            }
        }
        catch (JsonException)
        {
            // Non-JSON output cannot establish instruction-load evidence.
        }
    }

    sealed record StreamEvent(StreamMessage? Message);
    sealed record StreamMessage(StreamItem[]? Content);
    sealed record StreamItem(string? Type, string? Name, StreamInput? Input);
    sealed record StreamInput(string? FilePath);
}
