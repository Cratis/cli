// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Constraints;

/// <summary>
/// The outcome of waiting for a constraint reindex job.
/// </summary>
public enum ConstraintReindexOutcome
{
    /// <summary>The job completed successfully.</summary>
    Succeeded,

    /// <summary>The job failed or completed with failures.</summary>
    Failed,

    /// <summary>The job was stopped or is being removed, so it did not complete successfully.</summary>
    NotCompleted,

    /// <summary>The job could not be found, so its outcome is unknown.</summary>
    Missing,

    /// <summary>The jobs query failed or was refused, so the outcome is unknown.</summary>
    Unavailable,

    /// <summary>The job did not reach a terminal state before the deadline.</summary>
    TimedOut
}

/// <summary>
/// Polls the jobs query for a constraint reindex job until it reaches a terminal state. It never starts or restarts a job.
/// </summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Status">The last observed status, when the job was seen.</param>
/// <param name="Detail">Detail about a query failure, when there was one.</param>
public record ConstraintReindexResult(ConstraintReindexOutcome Outcome, JobStatus? Status = null, string? Detail = null);

/// <summary>
/// Waits for a constraint reindex job.
/// </summary>
public static class ConstraintReindexWait
{
    /// <summary>
    /// The number of consecutive polls that may find no job row before the job is reported missing.
    /// </summary>
    public const int MissingJobGracePolls = 5;

    /// <summary>
    /// Polls the jobs query until the job reaches a terminal state, is missing, the query fails or the deadline passes.
    /// </summary>
    /// <param name="jobs">The jobs service.</param>
    /// <param name="eventStore">The event store the job runs in.</param>
    /// <param name="namespace">The namespace the job runs in.</param>
    /// <param name="jobId">The job identifier returned when the reindex was accepted.</param>
    /// <param name="pollInterval">The delay between polls.</param>
    /// <param name="timeout">The longest time to wait.</param>
    /// <param name="cancellationToken">The token that stops waiting. It does not stop the server job.</param>
    /// <returns>The outcome.</returns>
    public static async Task<ConstraintReindexResult> Wait(
        IJobs jobs,
        string eventStore,
        string @namespace,
        Guid jobId,
        TimeSpan pollInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var missingPolls = 0;
        JobStatus? lastStatus = null;

        while (true)
        {
            var result = await jobs.AllJobs(new AllJobsRequest { EventStore = eventStore, Namespace = @namespace }, new ProtoBuf.Grpc.CallContext(new Grpc.Core.CallOptions(cancellationToken: cancellationToken)));
            if (!result.IsSuccess)
            {
                return new(ConstraintReindexOutcome.Unavailable, lastStatus, Describe(result));
            }

            var job = result.Data?.FirstOrDefault(_ => _.Id == jobId);
            if (job is null)
            {
                missingPolls++;
                if (missingPolls >= MissingJobGracePolls)
                {
                    return new(ConstraintReindexOutcome.Missing, lastStatus);
                }
            }
            else
            {
                missingPolls = 0;
                lastStatus = job.Status;
                switch (job.Status)
                {
                    case JobStatus.CompletedSuccessfully:
                        return new(ConstraintReindexOutcome.Succeeded, job.Status);
                    case JobStatus.CompletedWithFailures:
                    case JobStatus.Failed:
                        return new(ConstraintReindexOutcome.Failed, job.Status);
                    case JobStatus.Stopped:
                    case JobStatus.Removing:
                        return new(ConstraintReindexOutcome.NotCompleted, job.Status);
                }
            }

            if (DateTime.UtcNow + pollInterval >= deadline)
            {
                return new(ConstraintReindexOutcome.TimedOut, lastStatus);
            }

            await Task.Delay(pollInterval, cancellationToken);
        }
    }

    static string Describe<TData>(Cratis.Chronicle.Contracts.Queries.QueryResult<TData> result)
    {
        var messages = result.ExceptionMessages.Concat(result.ValidationResults.Select(_ => _.Message)).ToArray();
        if (messages.Length > 0)
        {
            return string.Join("; ", messages);
        }

        return result.IsAuthorized ? "the jobs query failed" : "access to the jobs query was denied";
    }
}
