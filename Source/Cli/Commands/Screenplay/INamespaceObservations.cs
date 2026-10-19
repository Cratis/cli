// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Defines the Chronicle reads a namespace observation is built from.
/// </summary>
/// <remarks>
/// Every read either returns the server's answer or throws; a refused or failed read never becomes an empty result.
/// </remarks>
public interface INamespaceObservations
{
    /// <summary>
    /// Gets the names of the event stores on the server.
    /// </summary>
    /// <returns>The event store names.</returns>
    Task<IReadOnlyList<string>> EventStores();

    /// <summary>
    /// Gets the namespaces of an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The namespace names.</returns>
    Task<IReadOnlyList<string>> Namespaces(string eventStore);

    /// <summary>
    /// Gets the event sequences of a namespace.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The event sequence identifiers.</returns>
    Task<IReadOnlyList<string>> EventSequences(string eventStore, string @namespace);

    /// <summary>
    /// Gets the highest used sequence number of an event sequence.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <returns>The tail, or <see langword="null"/> when the sequence holds no events.</returns>
    Task<ulong?> Tail(string eventStore, string @namespace, string eventSequenceId);

    /// <summary>
    /// Reads metadata, never payloads, for the events at the supplied sequence numbers.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="sequenceNumbers">At most <see cref="NamespaceObservationScan.PageSize"/> sequence numbers.</param>
    /// <returns>The metadata of the events that exist; missing numbers are omitted.</returns>
    Task<IReadOnlyList<ObservedEventMetadata>> Metadata(string eventStore, string @namespace, string eventSequenceId, IReadOnlyList<ulong> sequenceNumbers);

    /// <summary>
    /// Gets every registered event type generation of an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The registered event type generations.</returns>
    Task<IReadOnlyList<ObservedEventType>> RegisteredEventTypes(string eventStore);

    /// <summary>
    /// Gets the registered observers of a namespace as consumers.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The consumers.</returns>
    Task<IReadOnlyList<ObservedConsumer>> Consumers(string eventStore, string @namespace);
}
