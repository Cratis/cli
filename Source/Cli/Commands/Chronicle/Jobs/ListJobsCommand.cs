// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Lists all jobs (running and completed).
/// </summary>
[LlmDescription("Lists all background jobs including type, status, and progress. Jobs include replay, migration, and other long-running server tasks.")]
[CliCommand("list", "List all jobs", Branch = typeof(ChronicleBranch.Jobs))]
[CliExample("chronicle", "jobs", "list")]
public class ListJobsCommand : ChronicleCommand<JobsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, JobsSettings settings, string format)
    {
        var jobs = await services.Jobs.GetJobs(new GetJobsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace()
        });

        var jobList = (jobs ?? []).ToList();

        OutputFormatter.Write(
            format,
            jobList,
            ["Id", "Type", "Status", "Progress", "Created"],
            job =>
            [
                job.Id.ToString(),
                job.Type ?? string.Empty,
                job.Status.ToString(),
                FormatProgress(job.Progress),
                job.Created.ToString() ?? string.Empty
            ],
            job => job.Id.ToString());

        return ExitCodes.Success;
    }

    static string FormatProgress(JobProgress? progress)
    {
        if (progress is null)
        {
            return string.Empty;
        }

        if (progress.TotalSteps == 0)
        {
            return progress.Message ?? string.Empty;
        }

        return $"{progress.SuccessfulSteps}/{progress.TotalSteps}";
    }
}
