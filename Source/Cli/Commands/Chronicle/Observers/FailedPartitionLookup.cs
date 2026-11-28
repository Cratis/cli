// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;

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
    /// <remarks>
    /// Reading failed partitions needs read access to the store while recovering one needs only manage access, and manage does
    /// not imply read. When the server denies the read the partition cannot be checked, so the result is unverified and the
    /// caller submits the recovery as before, leaving the server to decide. Every other failure propagates.
    /// The read also does not canonicalize the namespace casing the way the recovery does, so when nothing is found the
    /// namespace is matched case-insensitively against the store's namespaces and the lookup repeated under the stored name.
    /// When that match is missing, denied or ambiguous the result is unverified rather than a refusal.
    /// </remarks>
    /// <param name="services">The <see cref="IServices"/> to query.</param>
    /// <param name="settings">The <see cref="PartitionCommandSettings"/> identifying the observer and partition.</param>
    /// <returns>
    /// A tuple where Verified is false when the server denied the lookup, and otherwise Partition is the
    /// <see cref="FailedPartition"/>, or null when the partition is not among the observer's failures.
    /// </returns>
    public static async Task<(bool Verified, FailedPartition? Partition)> Find(IServices services, PartitionCommandSettings settings)
    {
        var namespaceName = settings.ResolveNamespace();
        var (verified, partition) = await Read(services, settings, namespaceName);
        if (!verified || partition is not null)
        {
            return (verified, partition);
        }

        // The failed-partition read matches the namespace exactly while the recovery mutation canonicalizes its casing,
        // so a namespace in another letter case finds nothing here although the mutation would accept it.
        var canonical = await CanonicalNamespace(services, settings, namespaceName);
        if (canonical is null)
        {
            return (false, null);
        }

        return canonical == namespaceName ? (true, null) : await Read(services, settings, canonical);
    }

    static async Task<(bool Verified, FailedPartition? Partition)> Read(IServices services, PartitionCommandSettings settings, string namespaceName)
    {
        try
        {
            var failedPartitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
            {
                EventStore = settings.ResolveEventStore(),
                Namespace = namespaceName,
                ObserverId = settings.ObserverId
            });

            return (true, failedPartitions.FirstOrDefault(_ => _.ObserverId == settings.ObserverId && _.Partition == settings.Partition));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return (false, null);
        }
    }

    /// <summary>
    /// Finds the stored name of the namespace. Returns null when that cannot be established reliably: the namespaces cannot
    /// be read, none matches, or several match only by letter case.
    /// </summary>
    /// <param name="services">The <see cref="IServices"/> to query.</param>
    /// <param name="settings">The <see cref="PartitionCommandSettings"/> identifying the event store.</param>
    /// <param name="namespaceName">The namespace as given.</param>
    /// <returns>The stored namespace name, or null when it cannot be established.</returns>
    static async Task<string?> CanonicalNamespace(IServices services, PartitionCommandSettings settings, string namespaceName)
    {
        try
        {
            var result = await services.Namespaces.AllNamespaces(new AllNamespacesRequest { EventStore = settings.ResolveEventStore() });
            var names = (result.Data ?? []).Select(_ => _.Name).ToList();
            if (names.Contains(namespaceName))
            {
                return namespaceName;
            }

            var matches = names.Where(_ => string.Equals(_, namespaceName, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return null;
        }
    }
}
