// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Command to request irreversible deletion of a job and its retained state.
/// </summary>
[LlmDescription("Requests irreversible deletion of a job and its retained state. Prompts for confirmation unless --yes is specified. Authorization and policy belong to the server. Success means deletion requested, not confirmed removal. Server refusals and failures exit non-zero.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("delete", "Request irreversible deletion of a job and its retained state", Branch = typeof(ChronicleBranch.Jobs), DynamicCompletion = "jobs")]
[CliExample("chronicle", "jobs", "delete", "550e8400-e29b-41d4-a716-446655440000", "--yes")]
[LlmOption("<JOB_ID>", "guid", "Job identifier (positional)")]
public class DeleteJobCommand : ChronicleCommand<JobCommandSettings>
{
    /// <inheritdoc/>
    protected override int ResolveConfirmationExitCode(int exitCode) => exitCode == ExitCodes.Success ? ExitCodes.ValidationError : exitCode;

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(JobCommandSettings settings) =>
        $"Delete job [bold]{Markup.Escape(settings.JobId)}[/] from event store [bold]{Markup.Escape(settings.ResolveEventStore())}[/], namespace [bold]{Markup.Escape(settings.ResolveNamespace())}[/]? The retained job and its state are removed. This cannot be undone.";

    /// <inheritdoc/>
    protected override int? ExecuteOffline(JobCommandSettings settings)
    {
        if (Guid.TryParse(settings.JobId, out _))
        {
            return null;
        }

        OutputFormatter.WriteError(settings.ResolveOutputFormat(), $"Invalid job ID '{settings.JobId}' — must be a valid GUID", errorCode: ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, JobCommandSettings settings, string format)
    {
        if (!Guid.TryParse(settings.JobId, out var jobId))
        {
            OutputFormatter.WriteError(format, $"Invalid job ID '{settings.JobId}' — must be a valid GUID", errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var eventStore = settings.ResolveEventStore();
        var @namespace = settings.ResolveNamespace();
        var result = await services.Jobs.DeleteJob(new DeleteJobRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            JobId = jobId
        });
        if (HandleCommandResult(result, format) is { } failure)
        {
            return failure;
        }

        var message = $"Deletion requested for job {jobId}.";
        OutputFormatter.WriteObject(format, new { EventStore = eventStore, Namespace = @namespace, JobId = jobId, Message = message }, _ => OutputFormatter.WriteMessage(format, message));
        return ExitCodes.Success;
    }
}
