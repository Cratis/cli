// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.Commands.Chronicle.Jobs;

/// <summary>
/// Describes, in operator terms, why the kernel refused to stop or resume a job.
/// </summary>
/// <remarks>
/// The kernel answers a stop or resume with whether it accepted the request and, when it did not, why. A refusal is
/// a real answer - the job was left exactly as it was - so it is reported as a failure and never as success.
/// </remarks>
public static class JobCommandRefusals
{
    /// <summary>
    /// Gets a human-readable message and a suggested next step for a refusal.
    /// </summary>
    /// <param name="refusal">The refusal the kernel reported.</param>
    /// <param name="jobId">The job identifier the operator named.</param>
    /// <param name="action">The attempted action, as a verb such as <c language="text">stop</c> or <c language="text">resume</c>.</param>
    /// <returns>The message and the suggestion.</returns>
    public static (string Message, string Suggestion) Describe(JobCommandRefusal refusal, string jobId, string action) =>
        refusal switch
        {
            JobCommandRefusal.NotFound =>
                ($"Job '{jobId}' was not found, or is of a type this kernel does not know, so it was not {Past(action)}.",
                 "List the jobs to check the identifier: cratis chronicle jobs list"),

            JobCommandRefusal.JobIsCompleted =>
                ($"Job '{jobId}' has already concluded, so it was not {Past(action)}.",
                 $"Check its final state: cratis chronicle jobs get {jobId}"),

            JobCommandRefusal.JobIsNotRunning =>
                ($"Job '{jobId}' is not running, so there is nothing to {action}.",
                 $"Check its state: cratis chronicle jobs get {jobId}"),

            JobCommandRefusal.CouldNotPersist =>
                ($"The change to job '{jobId}' could not be made durable, so the job was left as it was and was not {Past(action)}.",
                 $"Retry the command; if it keeps failing, check the kernel's storage. Check the job: cratis chronicle jobs get {jobId}"),

            JobCommandRefusal.NotResumed =>
                ($"Nothing took job '{jobId}' forward: it was never prepared, or its owner is no longer interested in it, so it was not {Past(action)}.",
                 $"Check its state: cratis chronicle jobs get {jobId}"),

            _ =>
                ($"Job '{jobId}' was not {Past(action)}: the kernel refused ({refusal}).",
                 $"Check its state: cratis chronicle jobs get {jobId}")
        };

    /// <summary>
    /// Gets the exit code a refusal maps to: a server error when the change could not be persisted, otherwise a validation error.
    /// </summary>
    /// <param name="refusal">The refusal the kernel reported.</param>
    /// <returns>The exit code.</returns>
    public static int ExitCodeFor(JobCommandRefusal refusal) =>
        refusal == JobCommandRefusal.CouldNotPersist ? ExitCodes.ServerError : ExitCodes.ValidationError;

    /// <summary>
    /// Gets the machine-readable error code a refusal maps to.
    /// </summary>
    /// <param name="refusal">The refusal the kernel reported.</param>
    /// <returns>The error code.</returns>
    public static string ErrorCodeFor(JobCommandRefusal refusal) =>
        refusal == JobCommandRefusal.CouldNotPersist ? ExitCodes.ServerErrorCode : ExitCodes.ValidationErrorCode;

    /// <summary>
    /// Throws when a stop was not accepted, so an interactive caller reports a failure rather than success.
    /// </summary>
    /// <param name="result">The command result returned by the kernel.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <exception cref="InvalidOperationException">Thrown when the command failed or the kernel refused it.</exception>
    public static void EnsureAccepted(CommandResult<StopJobWithResultResponse> result, string jobId) =>
        EnsureAccepted(result.IsSuccess, result.IsSuccess && result.Response.Accepted, result.IsSuccess ? result.Response.Refusal : JobCommandRefusal.NotRefused, jobId, "stop");

    /// <summary>
    /// Throws when a resume was not accepted, so an interactive caller reports a failure rather than success.
    /// </summary>
    /// <param name="result">The command result returned by the kernel.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <exception cref="InvalidOperationException">Thrown when the command failed or the kernel refused it.</exception>
    public static void EnsureAccepted(CommandResult<ResumeJobWithResultResponse> result, string jobId) =>
        EnsureAccepted(result.IsSuccess, result.IsSuccess && result.Response.Accepted, result.IsSuccess ? result.Response.Refusal : JobCommandRefusal.NotRefused, jobId, "resume");

    static void EnsureAccepted(bool isSuccess, bool accepted, JobCommandRefusal refusal, string jobId, string action)
    {
        if (!isSuccess)
        {
            throw new InvalidOperationException($"The server failed to {action} job '{jobId}'");
        }

        if (!accepted)
        {
            throw new InvalidOperationException(Describe(refusal, jobId, action).Message);
        }
    }

    static string Past(string action) => action == "stop" ? "stopped" : "resumed";
}
