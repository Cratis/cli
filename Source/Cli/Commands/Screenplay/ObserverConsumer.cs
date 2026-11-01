// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Converts a registered Chronicle observer into the consumer evidence an observation snapshot holds.
/// </summary>
public static class ObserverConsumer
{
    const ulong FirstSentinel = ulong.MaxValue - 2;

    /// <summary>
    /// Converts one observer.
    /// </summary>
    /// <param name="observer">The registered observer.</param>
    /// <returns>The consumer subscription and progress, with sentinel positions omitted.</returns>
    public static ObservedConsumer From(ObserverInformation observer) => new(
        observer.Id,
        observer.Type switch
        {
            ObserverType.Reactor => ObservedConsumerKind.Reaction,
            ObserverType.Projection => ObservedConsumerKind.Projection,
            ObserverType.Reducer => ObservedConsumerKind.Reducer,
            _ => ObservedConsumerKind.Other
        },
        observer.Owner switch
        {
            ObserverOwner.Client => ObservedConsumerOwner.Application,
            ObserverOwner.Kernel => ObservedConsumerOwner.Platform,
            _ => ObservedConsumerOwner.Unknown
        },
        observer.EventSequenceId,
        [.. (observer.EventTypes ?? []).Select(type => new ObservedEventType(type.Id, type.Generation)).Distinct()],
        Position(observer.NextEventSequenceNumber),
        Position(observer.LastHandledEventSequenceNumber));

    /// <summary>
    /// Converts a Chronicle sequence number to a position, treating Chronicle's sentinel values as no position.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <returns>The position, or <see langword="null"/> for a sentinel.</returns>
    public static long? Position(ulong sequenceNumber) => sequenceNumber >= FirstSentinel || sequenceNumber > long.MaxValue ? null : (long)sequenceNumber;
}
