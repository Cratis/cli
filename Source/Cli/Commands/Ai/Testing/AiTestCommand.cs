// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai.Testing;

/// <summary>
/// Runs opt-in harness sessions and deterministic conformance checks in temporary workspaces.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("test", "Run deterministic AI conformance fixtures in isolated temporary workspaces. Runs real harness sessions only when explicitly invoked; may spend model quota. Installation and passing checks do not prove instructions were loaded.", Branch = typeof(AiBranch))]
[CliExample("ai", "test", "--source", "../AI", "--harnesses", "claude", "--output", "json")]
public sealed class AiTestCommand : AsyncCommand<AiTestSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AiTestSettings settings, CancellationToken cancellationToken)
    {
        var redactor = AiTestRedactor.FromEnvironment();
        try
        {
            settings.Source ??= Environment.GetEnvironmentVariable("CRATIS_AI_SOURCE");
            using var corpus = AiCorpusSource.Resolve(settings.Source);
            var process = new AiTestProcess();
            var runner = new AiTestRunner([new AiTestHarness("claude", process), new AiTestHarness("codex", process)], process, redactor);
            var report = await runner.Run(Directory.GetCurrentDirectory(), corpus.Path, settings, cancellationToken);
            return report.Write(settings.Output ?? "text");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or JsonException)
        {
            return new AiTestReport([], Error: redactor.Redact(exception.Message)).Write(settings.Output ?? "text");
        }
        catch (OperationCanceledException)
        {
            return new AiTestReport([new("run", "unknown", "error", "Cancelled.", null, "unknown", [], [], [])]).Write(settings.Output ?? "text");
        }
    }
}
