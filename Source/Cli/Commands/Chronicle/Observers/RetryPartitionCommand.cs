// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Retries a failed partition.
/// </summary>
[CliCommand("retry-partition", "Retry a failed partition", Branch = typeof(ChronicleBranch.Observers))]
[CliExample("chronicle", "observers", "retry-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
[LlmOption("<PARTITION>", "string", "Partition key (typically an event source ID, from 'cratis failed-partitions list') (positional)")]
public class RetryPartitionCommand : ChronicleCommand<PartitionCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, PartitionCommandSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to retry partition '{settings.Partition}' of observer '{settings.ObserverId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        await services.Observers.RetryPartition(new RetryPartitionContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId,
            Partition = settings.Partition
        });

        OutputFormatter.WriteMessage(format, $"Retry started for partition '{settings.Partition}' of observer '{settings.ObserverId}'. Use 'cratis observers show {settings.ObserverId}' to check progress.");
        return ExitCodes.Success;
    }
}
