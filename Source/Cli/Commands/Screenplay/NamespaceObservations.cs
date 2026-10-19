// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents an implementation of <see cref="INamespaceObservations"/> over the Chronicle gRPC services.
/// </summary>
/// <param name="services">The connected Chronicle services.</param>
public class NamespaceObservations(IServices services) : INamespaceObservations
{
    const ulong FirstSentinel = ulong.MaxValue - 2;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EventStores() =>
        [.. (Data(await services.EventStores.AllEventStores(), "event stores") ?? []).Select(store => store.Name)];

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> Namespaces(string eventStore) =>
        [.. (Data(await services.Namespaces.AllNamespaces(new() { EventStore = eventStore }), "namespaces") ?? []).Select(ns => ns.Name)];

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EventSequences(string eventStore, string @namespace) =>
        [.. (Data(await services.Sequences.AllEventSequences(new() { EventStore = eventStore, Namespace = @namespace }), "event sequences") ?? []).Select(sequence => sequence.Id)];

    /// <inheritdoc/>
    public async Task<ulong?> Tail(string eventStore, string @namespace, string eventSequenceId)
    {
        var tail = Data(await services.Sequences.TailSequenceNumber(new() { EventStore = eventStore, Namespace = @namespace, EventSequenceId = eventSequenceId }), "tail sequence number");
        return tail.SequenceNumber >= FirstSentinel ? null : tail.SequenceNumber;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ObservedEventMetadata>> Metadata(string eventStore, string @namespace, string eventSequenceId, IReadOnlyList<ulong> sequenceNumbers)
    {
        var request = new MetadataAtRequest { EventStore = eventStore, Namespace = @namespace, EventSequenceId = eventSequenceId, SequenceNumbers = sequenceNumbers };
        var metadata = Data(await services.Sequences.MetadataAt(request), "event metadata");
        return [.. (metadata ?? []).Select(entry => new ObservedEventMetadata(entry.SequenceNumber, entry.EventTypeId, CausationProducer.From(entry.Causation)))];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ObservedEventType>> RegisteredEventTypes(string eventStore) =>
        [.. (Data(await services.EventTypes.AllEventTypes(new() { EventStore = eventStore }), "event types") ?? []).SelectMany(Generations).Distinct()];

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ObservedConsumer>> Consumers(string eventStore, string @namespace) =>
        [.. (await services.Observers.GetObservers(new AllObserversRequest { EventStore = eventStore, Namespace = @namespace }) ?? []).Select(ObserverConsumer.From)];

    /// <summary>
    /// Expands a registration to every registered generation of its type.
    /// </summary>
    /// <remarks>
    /// The registry lists a type's latest generation; Chronicle registers every generation from 1 up to it
    /// (each needs a migration), so all of them count as registered.
    /// </remarks>
    /// <param name="registration">The event type registration.</param>
    /// <returns>One observed event type per generation.</returns>
    static IEnumerable<ObservedEventType> Generations(EventTypeDetailsResponse registration) =>
        Enumerable.Range(1, (int)Math.Max(registration.Type.Generation, 1u)).Select(generation => new ObservedEventType(registration.Type.Id, (uint)generation));

    static TData Data<TData>(QueryResult<TData> result, string operation) => result.IsSuccess
        ? result.Data
        : throw new ChronicleReadFailed(operation, !result.IsAuthorized, Describe(result));

    static string Describe<TData>(QueryResult<TData> result) => result switch
    {
        { ExceptionMessages.Count: > 0 } => string.Join("; ", result.ExceptionMessages),
        { ValidationResults.Count: > 0 } => string.Join("; ", result.ValidationResults.Select(validation => validation.Message)),
        _ => "not authorized"
    };
}
