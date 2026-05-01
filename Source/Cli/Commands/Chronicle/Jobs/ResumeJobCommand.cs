// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Resumes a stopped or failed job.
/// </summary>
[CliCommand("resume", "Resume a stopped or failed job", Branch = typeof(ChronicleBranch.Jobs), DynamicCompletion = "jobs")]
[CliExample("chronicle", "jobs", "resume", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<JOB_ID>", "guid", "Job identifier (positional)")]
public class ResumeJobCommand : ChronicleCommand<JobCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, JobCommandSettings settings, string format)
    {
        if (!Guid.TryParse(settings.JobId, out var jobId))
        {
            OutputFormatter.WriteError(format, $"Invalid job ID '{settings.JobId}' — must be a valid GUID", errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        await services.Jobs.Resume(new ResumeJob
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            JobId = jobId
        });

        OutputFormatter.WriteMessage(format, $"Job {settings.JobId} resumed successfully");
        return ExitCodes.Success;
    }
}
