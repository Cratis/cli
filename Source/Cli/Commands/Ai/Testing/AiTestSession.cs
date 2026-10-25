// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed record AiTestSessionConfiguration(AiConfiguration Configuration, string Corpus, AiTestSettings Settings);
internal sealed record AiTestExecution(AiTestFixture Fixture, AiTestWorkspace Workspace, AiTestBounds Bounds);

internal sealed class AiTestSession(IAiTestProcess process, AiTestRedactor redactor, AiTestSessionConfiguration configuration)
{
    internal async Task<AiTestResult> Run(AiTestFixtureSource source, IAiTestHarness? harness, string harnessId, CancellationToken cancellationToken)
    {
        var result = new AiTestResult(source.Fixture.Name, harnessId, "error", source.Error, null, AiCorpusSynchronizer.Revision(configuration.Corpus), [], [], []);
        try
        {
            if (source.Error is not null) return result;
            source.Fixture.Validate();
            var bounds = source.Fixture.Bounds.Lower(configuration.Settings.Timeout, configuration.Settings.MaxBudgetUsd);
            result = WithBounds(result, bounds, harness);
            if (Unsupported(harness)) return result with { Status = "skipped", Reason = $"Unsupported harness: {harnessId}" };
            using var workspace = new AiTestWorkspace { KeepTranscripts = configuration.Settings.KeepTranscripts };
            workspace.Prepare(source.Directory);
            result = Install(result, workspace.Path, configuration.Corpus, Slice(source.Fixture));
            if (result.ErrorKind is not null) return result;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(bounds.TimeoutSeconds));
            var execution = new AiTestExecution(source.Fixture, workspace, bounds);
            if (harness is null) return await InstallOnly(result, execution, deadline.Token);
            return await Execute(result, harness, execution, deadline.Token);
        }
        catch (OperationCanceledException)
        {
            return result with { Reason = cancellationToken.IsCancellationRequested ? "Cancelled." : "Fixture timeout exceeded." };
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or JsonException)
        {
            return result with { Reason = redactor.Redact(exception.Message) };
        }
    }

    static AiTestResult Install(AiTestResult result, string workspace, string corpus, AiConfiguration configuration)
    {
        try
        {
            var installation = AiCorpusSynchronizer.Synchronize(workspace, corpus, configuration with { Harnesses = [result.Harness], McpServers = null });
            if (installation.Conflicts.Count > 0) throw new InvalidOperationException($"Installation conflicts: {string.Join(", ", installation.Conflicts)}");
            var manifest = JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(Path.Combine(workspace, ".cratis", "ai.manifest.json")))!;
            return result with { Installed = [.. manifest.Files.Select(file => new AiTestInstalled($".cratis/ai/{file.Destination}", file.Hash))] };
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or JsonException)
        {
            return result with { ErrorKind = "installation", Reason = exception.Message };
        }
    }

    static string Status(IReadOnlyList<AiTestCheckResult> checks)
    {
        var required = checks.Where(check => !check.Optional).ToArray();
        if (required.Any(check => check.Status == "error")) return "error";
        return required.Any(check => check.Status == "failed") ? "failed" : "passed";
    }

    bool Unsupported(IAiTestHarness? harness) => harness is null && !configuration.Settings.InstallOnly;

    AiTestResult WithBounds(AiTestResult result, AiTestBounds bounds, IAiTestHarness? harness)
    {
        if (configuration.Settings.InstallOnly) return result with { Bounds = AiTestBoundEnforcement.NotApplicable(bounds), ResultPhase = "not_run" };
        return result with { Bounds = AiTestBoundEnforcement.For(bounds, harness?.Capabilities ?? AiTestHarnessCapabilities.None) };
    }

    AiConfiguration Slice(AiTestFixture fixture) => configuration.Configuration with
    {
        Profiles = Select(fixture.Profiles, configuration.Configuration.Profiles),
        Languages = Select(fixture.Languages, configuration.Configuration.Languages)
    };

    IReadOnlyList<string> Select(string[] declared, IReadOnlyList<string> invoking)
    {
        if (declared.Length == 0) return invoking;
        if (configuration.Settings.Source is not null) return declared;
        return [.. declared.Intersect(invoking, StringComparer.Ordinal)];
    }

    async Task<AiTestResult> InstallOnly(AiTestResult result, AiTestExecution execution, CancellationToken cancellationToken)
    {
        var checks = await Evaluate(execution, AiTestPhase.Install, new AiTestChecks(process, redactor), cancellationToken);
        if (checks.Count == 0) return result with { Status = "skipped", Reason = "no install-phase checks" };
        var status = Status(checks);
        return result with { Status = status, Reason = status == "passed" ? null : "Install-phase checks did not pass.", Checks = checks };
    }

    async Task<AiTestResult> Execute(AiTestResult result, IAiTestHarness harness, AiTestExecution execution, CancellationToken cancellationToken)
    {
        var evaluator = new AiTestChecks(process, redactor);
        var checks = await Evaluate(execution, AiTestPhase.Install, evaluator, cancellationToken);
        if (Status(checks) != "passed") return result with { Status = Status(checks), Reason = "Install-phase checks did not pass.", Checks = checks };
        var outcome = await harness.Run(execution.Workspace.Path, execution.Fixture.Prompt, execution.Bounds, cancellationToken);
        var transcript = await SaveTranscript(outcome.Transcript, execution.Workspace, cancellationToken);
        if (outcome.Status == "passed") checks.AddRange(await Evaluate(execution, AiTestPhase.Result, evaluator, cancellationToken));
        return result with
        {
            Status = outcome.Status == "passed" ? Status(checks) : outcome.Status,
            Reason = redactor.Redact(outcome.Reason ?? string.Empty),
            Version = redactor.Redact(outcome.Version ?? "unknown"),
            Checks = checks,
            TranscriptPath = transcript,
            ObservedLoad = AiTestObservedLoads.Read(outcome.Transcript, execution.Workspace.Path, execution.Fixture.ExpectedInstructionPaths, harness.Capabilities)
        };
    }

    async Task<List<AiTestCheckResult>> Evaluate(AiTestExecution execution, AiTestPhase phase, AiTestChecks evaluator, CancellationToken cancellationToken)
    {
        var checks = new List<AiTestCheckResult>();
        foreach (var check in execution.Fixture.Checks.Where(check => check.Phase == phase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            checks.Add(await evaluator.Evaluate(check, execution.Workspace.Path, TimeSpan.FromSeconds(execution.Bounds.TimeoutSeconds), cancellationToken));
        }
        return checks;
    }

    async Task<string?> SaveTranscript(string transcript, AiTestWorkspace workspace, CancellationToken cancellationToken)
    {
        if (!workspace.KeepTranscripts) return null;
        Directory.CreateDirectory(workspace.Output);
        var path = Path.Combine(workspace.Output, "transcript.txt");
        await File.WriteAllTextAsync(path, redactor.Redact(transcript), cancellationToken);
        return path;
    }
}
