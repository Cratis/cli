// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Retries a failed partition.
/// </summary>
/// <remarks>
/// This used to print "Retry started" and exit zero whatever happened, including for the cases the kernel declines
/// outright - a quarantined observer has recovery paused by design, an individually quarantined partition is not
/// retried until it is cleared, and a partition that is not among the observer's failures has nothing to recover. An
/// operator recovering a stuck store was told to go watch progress that was never coming. The kernel reports which of
/// those it did, so the command says so and exits non-zero when nothing was started.
/// </remarks>
[LlmDescription("Retries a failed partition of an observer from the sequence number where it last failed. Use to recover a failed partition after the underlying issue is resolved. Exits non-zero when the retry is refused.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("retry-partition", "Retry a failed partition", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "retry-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
[LlmOption("<PARTITION>", "string", "Partition key (typically an event source ID, from 'cratis failed-partitions list') (positional)")]
public class RetryPartitionCommand : ChronicleCommand<PartitionCommandSettings>
{
    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(PartitionCommandSettings settings) =>
        $"Are you sure you want to retry partition '{settings.Partition}' of observer '{settings.ObserverId}'?";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, PartitionCommandSettings settings, string format)
    {
        var response = await services.Observers.RetryPartition(new RetryPartitionContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId,
            Partition = settings.Partition
        });

        if (response.Outcome == PartitionRecoveryOutcome.Started)
        {
            OutputFormatter.WriteMessage(format, $"Retry started for partition '{settings.Partition}' of observer '{settings.ObserverId}'. Use 'cratis chronicle observers show {settings.ObserverId}' to check progress.");
            return ExitCodes.Success;
        }

        var (message, suggestion) = DescribeRefusal(response.Outcome, settings);
        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    static (string Message, string Suggestion) DescribeRefusal(PartitionRecoveryOutcome outcome, PartitionCommandSettings settings) =>
        outcome switch
        {
            PartitionRecoveryOutcome.ObserverQuarantined =>
                ($"Observer '{settings.ObserverId}' is quarantined, so recovery is paused and partition '{settings.Partition}' was not retried.",
                 $"Clear the observer quarantine first: cratis chronicle observers clear-quarantine {settings.ObserverId}"),

            PartitionRecoveryOutcome.PartitionQuarantined =>
                ($"Partition '{settings.Partition}' of observer '{settings.ObserverId}' is quarantined after exhausting its retry attempts, so it was not retried.",
                 "Clear the partition quarantine before retrying it."),

            PartitionRecoveryOutcome.PartitionNotFound =>
                ($"Partition '{settings.Partition}' is not among the failed partitions of observer '{settings.ObserverId}', so there was nothing to retry.",
                 $"List the failed partitions to check the key: cratis chronicle failed-partitions list --observer {settings.ObserverId}"),

            _ =>
                ($"Partition '{settings.Partition}' of observer '{settings.ObserverId}' was not retried: {outcome}.",
                 "Check the observer state with 'cratis chronicle observers show'.")
        };
}
