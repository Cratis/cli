// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Looks up the failed partition a recovery command targets.
/// </summary>
/// <remarks>
/// The kernel queues partition recovery as an operator-command job and always answers "accepted" without checking the
/// partition, so the command has to check the observer's failed partitions itself before it submits anything.
/// </remarks>
static class FailedPartitionLookup
{
    /// <summary>
    /// Finds the partition among the failed partitions of the observer.
    /// </summary>
    /// <param name="services">The <see cref="IServices"/> to query.</param>
    /// <param name="settings">The <see cref="PartitionCommandSettings"/> identifying the observer and partition.</param>
    /// <returns>The <see cref="FailedPartition"/>, or null when the partition is not among the observer's failures.</returns>
    public static async Task<FailedPartition?> Find(IServices services, PartitionCommandSettings settings)
    {
        var failedPartitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId
        });

        return failedPartitions.FirstOrDefault(_ => _.ObserverId == settings.ObserverId && _.Partition == settings.Partition);
    }
}
