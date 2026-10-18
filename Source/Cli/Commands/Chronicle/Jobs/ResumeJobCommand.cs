// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Resumes a stopped job.
/// </summary>
[LlmDescription("Resumes a stopped background job. Use after stopping a job. A failed job cannot be resumed. Prompts for confirmation unless --yes is specified. Exits non-zero, naming the reason, when the kernel refuses (job not found, not persisted, nothing took it forward).")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("resume", "Resume a stopped job", Branch = typeof(ChronicleBranch.Jobs), DynamicCompletion = "jobs")]
[CliExample("chronicle", "jobs", "resume", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<JOB_ID>", "guid", "Job identifier (positional)")]
public class ResumeJobCommand : ChronicleCommand<JobCommandSettings>
{
    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(JobCommandSettings settings) =>
        $"Are you sure you want to resume job '{settings.JobId}'?";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, JobCommandSettings settings, string format)
    {
        if (!Guid.TryParse(settings.JobId, out var jobId))
        {
            OutputFormatter.WriteError(format, $"Invalid job ID '{settings.JobId}' — must be a valid GUID", errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var result = await services.Jobs.ResumeJobWithResult(new ResumeJobWithResultRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            JobId = jobId
        });

        if (HandleCommandResult(result, format) is { } exitCode)
        {
            return exitCode;
        }

        if (!result.Response.Accepted)
        {
            var (message, suggestion) = JobCommandRefusals.Describe(result.Response.Refusal, settings.JobId, "resume");
            OutputFormatter.WriteError(format, message, suggestion, JobCommandRefusals.ErrorCodeFor(result.Response.Refusal));
            return JobCommandRefusals.ExitCodeFor(result.Response.Refusal);
        }

        OutputFormatter.WriteMessage(format, $"Job {settings.JobId} resumed successfully");
        return ExitCodes.Success;
    }
}
