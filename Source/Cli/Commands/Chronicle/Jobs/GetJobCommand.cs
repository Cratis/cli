// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Shows detailed information about a specific job, including its steps.
/// </summary>
[LlmDescription("Shows detailed information about a specific background job including its status, progress, and error details if failed. Use -o json-compact.")]
[CliCommand("get", "Show detailed information about a specific job", Branch = typeof(ChronicleBranch.Jobs), DynamicCompletion = "jobs")]
[CliExample("chronicle", "jobs", "get", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<JOB_ID>", "guid", "Job identifier (positional)")]
public class GetJobCommand : ChronicleCommand<JobCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, JobCommandSettings settings, string format)
    {
        if (!Guid.TryParse(settings.JobId, out var jobId))
        {
            OutputFormatter.WriteError(format, $"Invalid job ID '{settings.JobId}' — must be a valid GUID", errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var result = await services.Jobs.GetJob(new GetJobRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            JobId = jobId
        });

        var job = result.Value0;

        if (job is null)
        {
            OutputFormatter.WriteError(
                format,
                $"Job '{settings.JobId}' not found",
                "Use 'cratis chronicle jobs list' to see all jobs",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var steps = await services.Jobs.GetJobSteps(new GetJobStepsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            JobId = jobId
        });

        OutputFormatter.WriteObject(
            format,
            new
            {
                id = job.Id,
                type = job.Type,
                status = job.Status.ToString(),
                created = job.Created.ToString(),
                progress = new
                {
                    totalSteps = job.Progress?.TotalSteps,
                    successfulSteps = job.Progress?.SuccessfulSteps,
                    failedSteps = job.Progress?.FailedSteps,
                    isCompleted = job.Progress?.IsCompleted,
                    message = job.Progress?.Message
                },
                steps = (steps ?? []).Select(s => new
                {
                    id = s.Id,
                    type = s.Type,
                    name = s.Name,
                    status = s.Status.ToString()
                }).ToArray()
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]Job:[/]      {data.id}");
                AnsiConsole.MarkupLine($"[bold]Type:[/]     {(data.type ?? string.Empty).EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Status:[/]   {data.status}");
                AnsiConsole.MarkupLine($"[bold]Created:[/]  {(data.created ?? string.Empty).EscapeMarkup()}");

                if (data.progress.totalSteps > 0)
                {
                    AnsiConsole.MarkupLine($"[bold]Progress:[/] {data.progress.successfulSteps}/{data.progress.totalSteps} steps");
                }

                if (!string.IsNullOrEmpty(data.progress.message))
                {
                    AnsiConsole.MarkupLine($"[bold]Message:[/]  {data.progress.message.EscapeMarkup()}");
                }

                if (data.steps.Length > 0)
                {
                    AnsiConsole.WriteLine();
                    OutputFormatter.WriteSection("Steps");
                    OutputFormatter.Write(
                        format,
                        data.steps,
                        ["Id", "Type", "Name", "Status"],
                        s => [s.id.ToString(), s.type ?? string.Empty, s.name ?? string.Empty, s.status]);
                }
            });

        return ExitCodes.Success;
    }
}
