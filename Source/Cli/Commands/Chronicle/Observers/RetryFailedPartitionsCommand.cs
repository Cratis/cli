// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RetryFailedPartitionsContract = Cratis.Chronicle.Contracts.Observation.RetryFailedPartitions;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Retries all failed partitions of an observer in one operation.
/// </summary>
/// <remarks>
/// The kernel starts recovery a bounded batch of partitions at a time, so retrying many of them never overwhelms the
/// observer or the event store. The command returns as soon as the kernel has accepted the request and reports how
/// many partitions were failed at that moment; it does not wait for recovery to finish.
/// </remarks>
[LlmDescription("Retries every failed partition of an observer in one operation, instead of retrying them one at a time. The kernel recovers a bounded batch of partitions at a time, so retrying many never overwhelms the observer; the command returns once recovery is started, reporting how many partitions were failed, and does not wait for it to finish. Check progress with 'failed-partitions list'. Prompts for confirmation unless --yes is specified.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("retry-failed-partitions", "Retry all failed partitions of an observer", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "retry-failed-partitions", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis chronicle observers list') (positional)")]
public class RetryFailedPartitionsCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ObserverCommandSettings settings) =>
        $"Are you sure you want to retry all failed partitions of observer '{settings.ObserverId}'?";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        var response = await services.Observers.RetryFailedPartitions(new RetryFailedPartitionsContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        var message = response.FailedPartitions == 0
            ? $"Observer '{settings.ObserverId}' has no failed partitions, so there was nothing to retry."
            : $"Retry started for {response.FailedPartitions} failed partition{(response.FailedPartitions == 1 ? string.Empty : "s")} of observer '{settings.ObserverId}', a bounded batch at a time. Use 'cratis chronicle failed-partitions list --observer {settings.ObserverId}' to check progress.";

        OutputFormatter.WriteObject(
            format,
            new { observerId = settings.ObserverId, failedPartitions = response.FailedPartitions, message },
            _ => OutputFormatter.WriteMessage(format, message));
        return ExitCodes.Success;
    }
}
