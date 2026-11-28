// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events.Constraints.Operator;

namespace Cratis.Cli.Commands.Chronicle.Constraints;

/// <summary>
/// Rebuilds unique and closing constraint indexes from an event sequence.
/// </summary>
[LlmDescription("Rebuilds unique and closing constraint indexes from an event sequence as a server-side job. Pass constraint names to rebuild only those; with no names it rebuilds all applicable unique and closing constraints for the sequence. Needs a Chronicle administrator. Governed appends to the sequence are refused while the job runs; clients should retry those appends after it ends. Prompts for confirmation unless --yes is specified. With --wait it polls the job and exits non-zero unless it completed successfully (needs Chronicle 19.196.0 or later for a reliable outcome). Use -o json-compact.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("reindex", "Rebuild unique and closing constraint indexes from an event sequence", Branch = typeof(ChronicleBranch.Constraints))]
[CliExample("chronicle", "constraints", "reindex")]
[CliExample("chronicle", "constraints", "reindex", "--event-store", "orders", "--namespace", "West", "--event-sequence", "event-log", "UniqueOrderNumber", "ClosedOrder", "--wait", "--yes", "--output", "json")]
[LlmOption("[CONSTRAINT_NAME]", "string", "Constraint names to rebuild; omit for all applicable unique and closing constraints (positional, repeatable)")]
[LlmOption("--event-sequence", "string", "Event sequence to rebuild from (default: event-log)")]
[LlmOption("--wait", "boolean", "Wait for the reindex job to finish; exits non-zero unless it completed successfully")]
public class ReindexConstraintsCommand : ChronicleCommand<ReindexConstraintsSettings>
{
    const string AllConstraints = "all applicable unique and closing constraints";

    Guid? _acceptedJobId;

    /// <inheritdoc/>
    protected override bool UseStatusSpinner => false;

    /// <inheritdoc/>
    protected override bool RequiresAdministrator => true;

    /// <summary>
    /// Gets the delay between polls while waiting.
    /// </summary>
    protected virtual TimeSpan PollInterval => TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the longest time to wait for the job.
    /// </summary>
    protected virtual TimeSpan WaitTimeout => TimeSpan.FromMinutes(30);

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ReindexConstraintsSettings settings) =>
        $"Rebuild {Describe(settings.ConstraintNames)} for event sequence '{settings.EventSequenceId}' in event store '{settings.ResolveEventStore()}', namespace '{settings.ResolveNamespace()}'? " +
        "Governed appends to the sequence are refused while the job runs; retry them after it ends.";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteConnectedAsync(ChronicleConnectionString connectionString, ReindexConstraintsSettings settings, string format, CancellationToken cancellationToken)
    {
        using var client = await CliChronicleConnection.Connect(connectionString, cancellationToken);
        return await Reindex(client.Services, settings, format, cancellationToken);
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteCommandAsync(IServices services, ReindexConstraintsSettings settings, string format) =>
        Reindex(services, settings, format, CancellationToken.None);

    /// <summary>
    /// Starts the reindex and, when asked, waits for the job.
    /// </summary>
    /// <param name="services">The Chronicle services.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="format">The resolved output format.</param>
    /// <param name="cancellationToken">The token that stops waiting; it never stops the server job.</param>
    /// <returns>The exit code.</returns>
    protected async Task<int> Reindex(IServices services, ReindexConstraintsSettings settings, string format, CancellationToken cancellationToken)
    {
        var eventStore = settings.ResolveEventStore();
        var @namespace = settings.ResolveNamespace();

        // After an accepted start, a re-entry (for example a refreshed login) only resumes waiting: the reindex is never submitted twice.
        if (_acceptedJobId is null)
        {
            var result = await services.ConstraintIndexes.ReindexConstraints(
                new ReindexConstraintsRequest
                {
                    EventStore = eventStore,
                    Namespace = @namespace,
                    EventSequenceId = settings.EventSequenceId,
                    ConstraintNames = settings.ConstraintNames
                });

            if (HandleCommandResult(result, format, true) is { } exitCode)
            {
                return exitCode;
            }

            _acceptedJobId = result.Response;
        }

        var jobId = _acceptedJobId.Value;
        if (!settings.Wait)
        {
            WriteStarted(format, settings, jobId, "Started");
            return ExitCodes.Success;
        }

        if (!IsMachineReadable(format))
        {
            WriteStarted(format, settings, jobId, "Started");
        }

        var outcome = await ConstraintReindexWait.Wait(services.Jobs, eventStore, @namespace, jobId, PollInterval, WaitTimeout, cancellationToken);
        return ReportOutcome(format, settings, jobId, outcome);
    }

    static string Describe(string[] names) => names.Length == 0 ? AllConstraints : string.Join(", ", names);

    static bool IsMachineReadable(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal);

    static string JobName(ReindexConstraintsSettings settings) => $"Reindex constraints for {settings.EventSequenceId}";

    int ReportOutcome(string format, ReindexConstraintsSettings settings, Guid jobId, ConstraintReindexResult outcome)
    {
        var status = outcome.Status?.ToString();
        var inspect = $"Inspect it with 'cratis chronicle jobs get {jobId}'";
        switch (outcome.Outcome)
        {
            case ConstraintReindexOutcome.Succeeded:
                if (IsMachineReadable(format))
                {
                    WriteStarted(format, settings, jobId, status!);
                }
                else
                {
                    OutputFormatter.WriteMessage(format, $"Reindex job {jobId} completed successfully");
                }

                return ExitCodes.Success;
            case ConstraintReindexOutcome.Failed:
                OutputFormatter.WriteError(format, $"Reindex job {jobId} did not succeed: {status}", inspect, ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            case ConstraintReindexOutcome.NotCompleted:
                OutputFormatter.WriteError(format, $"Reindex job {jobId} did not complete successfully: it was {status}", inspect, ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            case ConstraintReindexOutcome.Missing:
                return Indeterminate(format, jobId, "the job was not found in the jobs list. Chronicle before 19.196.0 may not keep successful jobs");
            case ConstraintReindexOutcome.Cancelled:
                return Indeterminate(format, jobId, "waiting was cancelled; the job keeps running on the server");
            case ConstraintReindexOutcome.Unavailable:
                return Indeterminate(format, jobId, $"the jobs query was unavailable ({outcome.Detail})");
            default:
                return Indeterminate(format, jobId, $"the job had not finished after {WaitTimeout}");
        }
    }

    int Indeterminate(string format, Guid jobId, string reason)
    {
        OutputFormatter.WriteError(
            format,
            $"Unable to verify the outcome of reindex job {jobId}: {reason}",
            $"The reindex was started and is not resubmitted. Check it with 'cratis chronicle jobs get {jobId}'",
            ExitCodes.InconclusiveCode);
        return ExitCodes.Inconclusive;
    }

    void WriteStarted(string format, ReindexConstraintsSettings settings, Guid jobId, string status) =>
        OutputFormatter.WriteObject(
            format,
            new
            {
                eventSequenceId = settings.EventSequenceId,
                constraintNames = settings.ConstraintNames,
                allConstraints = settings.ConstraintNames.Length == 0,
                jobId,
                jobName = JobName(settings),
                status
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]{JobName(settings).EscapeMarkup()}[/] started");
                AnsiConsole.MarkupLine($"[bold]Job:[/]         {data.jobId}");
                AnsiConsole.MarkupLine($"[bold]Constraints:[/] {Describe(data.constraintNames).EscapeMarkup()}");
            });
}
