// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

/// <summary>
/// Holds the results of a single diagnostic sweep of the Chronicle server.
/// </summary>
/// <param name="ConnectionString">The resolved connection string (credentials redacted in display).</param>
/// <param name="EventStore">The resolved event store name.</param>
/// <param name="Namespace">The resolved namespace name.</param>
/// <param name="ServerReachable">Whether the server responded to the connection attempt.</param>
/// <param name="ServerVersion">The server version string, or null when unreachable.</param>
/// <param name="EventStores">The list of event stores on the server.</param>
/// <param name="TotalObservers">Total number of observers in the event store and namespace.</param>
/// <param name="ActiveObservers">Number of observers in the Active state.</param>
/// <param name="FailingObservers">Number of observers in an unhealthy state (Disconnected or Unknown).</param>
/// <param name="SuspendedObservers">Number of observers in the Suspended state.</param>
/// <param name="FailedPartitions">Number of failed partitions requiring attention.</param>
/// <param name="PendingRecommendations">Number of pending system recommendations.</param>
/// <param name="EventSequenceTail">The tail (highest) sequence number of the event log, or null if unavailable.</param>
/// <param name="CapturedAt">The point in time this snapshot was captured.</param>
public record DiagnoseData(
    string ConnectionString,
    string EventStore,
    string Namespace,
    bool ServerReachable,
    string? ServerVersion,
    IReadOnlyList<string> EventStores,
    int TotalObservers,
    int ActiveObservers,
    int FailingObservers,
    int SuspendedObservers,
    int FailedPartitions,
    int PendingRecommendations,
    ulong? EventSequenceTail,
    DateTimeOffset CapturedAt)
{
    /// <summary>
    /// Gets a value indicating whether the system is healthy (no failures, server reachable).
    /// </summary>
    public bool IsHealthy =>
        ServerReachable &&
        FailingObservers == 0 &&
        FailedPartitions == 0;
}
