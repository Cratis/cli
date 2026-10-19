// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Observations;
using Grpc.Core;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents an implementation of <see cref="INamespaceObservations"/> over the Chronicle gRPC services.
/// </summary>
/// <param name="services">The connected Chronicle services.</param>
public class NamespaceObservations(IServices services) : INamespaceObservations
{
    const ulong FirstSentinel = ulong.MaxValue - 2;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EventStores(CancellationToken cancellationToken) =>
        [.. (await Read(() => services.EventStores.AllEventStores(cancellationToken), "event stores", cancellationToken) ?? []).Select(store => store.Name)];

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> Namespaces(string eventStore, CancellationToken cancellationToken) =>
        [.. (await Read(() => services.Namespaces.AllNamespaces(new() { EventStore = eventStore }, cancellationToken), "namespaces", cancellationToken) ?? []).Select(ns => ns.Name)];

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EventSequences(string eventStore, string @namespace, CancellationToken cancellationToken) =>
        [.. (await Read(() => services.Sequences.AllEventSequences(new() { EventStore = eventStore, Namespace = @namespace }, cancellationToken), "event sequences", cancellationToken) ?? []).Select(sequence => sequence.Id)];

    /// <inheritdoc/>
    public async Task<ulong?> Tail(string eventStore, string @namespace, string eventSequenceId, CancellationToken cancellationToken)
    {
        var tail = await Read(() => services.Sequences.TailSequenceNumber(new() { EventStore = eventStore, Namespace = @namespace, EventSequenceId = eventSequenceId }, cancellationToken), "tail sequence number", cancellationToken);
        return tail.SequenceNumber >= FirstSentinel ? null : tail.SequenceNumber;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ObservedEventMetadata>> Metadata(string eventStore, string @namespace, string eventSequenceId, IReadOnlyList<ulong> sequenceNumbers, CancellationToken cancellationToken)
    {
        var request = new MetadataAtRequest { EventStore = eventStore, Namespace = @namespace, EventSequenceId = eventSequenceId, SequenceNumbers = sequenceNumbers };
        var metadata = await Read(() => services.Sequences.MetadataAt(request, cancellationToken), "event metadata", cancellationToken);
        return [.. (metadata ?? []).Select(entry => new ObservedEventMetadata(entry.SequenceNumber, entry.EventTypeId, CausationProducer.From(entry.Causation)))];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ObservedEventType>> RegisteredEventTypes(string eventStore, CancellationToken cancellationToken) =>
        [.. (await Read(() => services.EventTypes.AllEventTypes(new() { EventStore = eventStore }, cancellationToken), "event types", cancellationToken) ?? []).SelectMany(Generations).Distinct()];

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ObservedConsumer>> Consumers(string eventStore, string @namespace, CancellationToken cancellationToken)
    {
        var observers = await Call(() => services.Observers.GetObservers(new AllObserversRequest { EventStore = eventStore, Namespace = @namespace }, cancellationToken), "observers", cancellationToken);
        return [.. (observers ?? []).Select(ObserverConsumer.From)];
    }

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

    static async Task<TData> Read<TData>(Func<Task<QueryResult<TData>>> read, string operation, CancellationToken cancellationToken) =>
        Data(await Call(read, operation, cancellationToken), operation);

    /// <summary>
    /// Runs a read, mapping a transport-level refusal to the same failure a refused query result produces.
    /// </summary>
    /// <remarks>
    /// A cancelled call is cancellation, not a read failure, so it surfaces as an <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="read">The read to run.</param>
    /// <param name="operation">The name of the read, for the failure message.</param>
    /// <param name="cancellationToken">The cancellation token the read was started with.</param>
    /// <returns>The result of the read.</returns>
    /// <exception cref="OperationCanceledException">The read was cancelled.</exception>
    /// <exception cref="ChronicleReadFailed">The server refused the read for lack of authorization.</exception>
    static async Task<TResult> Call<TResult>(Func<Task<TResult>> read, string operation, CancellationToken cancellationToken)
    {
        try
        {
            return await read();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.PermissionDenied or StatusCode.Unauthenticated)
        {
            throw new ChronicleReadFailed(operation, true, exception.Status.Detail);
        }
    }

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
