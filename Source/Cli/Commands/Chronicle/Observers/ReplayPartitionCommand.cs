// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ReplayPartitionContract = Cratis.Chronicle.Contracts.Observation.ReplayPartition;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Replays a specific partition of an observer.
/// </summary>
[CliCommand("replay-partition", "Replay a specific partition of an observer", Branch = typeof(ChronicleBranch.Observers))]
[CliExample("chronicle", "observers", "replay-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
[LlmOption("<PARTITION>", "string", "Partition key (typically an event source ID) (positional)")]
public class ReplayPartitionCommand : ChronicleCommand<PartitionCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, PartitionCommandSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to replay partition '{settings.Partition}' of observer '{settings.ObserverId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        await services.Observers.ReplayPartition(new ReplayPartitionContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId,
            Partition = settings.Partition
        });

        OutputFormatter.WriteMessage(format, $"Replay started for partition '{settings.Partition}' of observer '{settings.ObserverId}'");
        return ExitCodes.Success;
    }
}
