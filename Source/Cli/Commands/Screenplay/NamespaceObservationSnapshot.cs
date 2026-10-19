// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Builds the immutable observation snapshot of one namespace and event sequence from Chronicle reads.
/// </summary>
/// <remarks>
/// The sequence tail is captured once, before anything else is read, and the scan never reads past it, so events
/// appended during the scan cannot shift the coverage. Only event metadata is read. An appended event counts against
/// the highest registered generation of its type, which is the generation Chronicle presents stored events as; an
/// event of an unregistered type counts as generation 1.
/// </remarks>
public static class NamespaceObservationSnapshot
{
    /// <summary>
    /// Reads the namespace and builds its snapshot.
    /// </summary>
    /// <param name="observations">The Chronicle reads.</param>
    /// <param name="target">The event store, namespace and event sequence to observe.</param>
    /// <param name="scan">The bounds of the scan.</param>
    /// <param name="capturedAt">The capture time recorded in the snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapshot.</returns>
    public static async Task<ObservationSnapshot> Build(INamespaceObservations observations, NamespaceTarget target, NamespaceObservationScan scan, DateTimeOffset capturedAt, CancellationToken cancellationToken)
    {
        var tail = await observations.Tail(target.EventStore, target.Namespace, target.EventSequenceId);
        var registered = await observations.RegisteredEventTypes(target.EventStore);
        var consumers = await observations.Consumers(target.EventStore, target.Namespace);
        var tally = new Tally(registered);
        var coverage = await Scan(observations, target, scan, tail, tally, cancellationToken);

        return ObservationSnapshot.Create(target.EventStore, target.Namespace, coverage, registered, tally.Appends, tally.Edges, tally.Unattributed, consumers, capturedAt);
    }

    static async Task<ObservedSequenceCoverage> Scan(INamespaceObservations observations, NamespaceTarget target, NamespaceObservationScan scan, ulong? tail, Tally tally, CancellationToken cancellationToken)
    {
        var first = scan.From ?? 0;
        var startsAtFirstEvent = first == 0;
        if (tail is null)
        {
            return new(target.EventSequenceId, true, null, null, null, []);
        }

        var end = Math.Min(scan.To ?? tail.Value, tail.Value);
        if (first > end)
        {
            return new(target.EventSequenceId, startsAtFirstEvent, null, null, checked((long)tail.Value), []);
        }

        var last = end;
        for (var page = first; ; page += NamespaceObservationScan.PageSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageEnd = end - page < NamespaceObservationScan.PageSize ? end : page + NamespaceObservationScan.PageSize - 1;
            if (await ReadPage(observations, target, page, pageEnd, scan.MaxEvents, tally) is { } stoppedBefore)
            {
                last = stoppedBefore - 1;
                break;
            }

            if (pageEnd == end)
            {
                break;
            }
        }

        return new(target.EventSequenceId, startsAtFirstEvent, checked((long)first), checked((long)last), checked((long)tail.Value), []);
    }

    static async Task<ulong?> ReadPage(INamespaceObservations observations, NamespaceTarget target, ulong page, ulong pageEnd, int maxEvents, Tally tally)
    {
        var numbers = Enumerable.Range(0, (int)(pageEnd - page) + 1).Select(offset => page + (ulong)offset).ToArray();
        var events = (await observations.Metadata(target.EventStore, target.Namespace, target.EventSequenceId, numbers))
            .Where(metadata => metadata.SequenceNumber >= page && metadata.SequenceNumber <= pageEnd)
            .DistinctBy(metadata => metadata.SequenceNumber)
            .OrderBy(metadata => metadata.SequenceNumber);
        foreach (var metadata in events)
        {
            if (tally.Events == maxEvents)
            {
                return metadata.SequenceNumber;
            }

            tally.Add(metadata);
        }

        return null;
    }

    sealed class Tally(IEnumerable<ObservedEventType> registered)
    {
        readonly Dictionary<string, uint> _generations = registered.GroupBy(type => type.TypeId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Max(type => type.Generation), StringComparer.Ordinal);
        readonly Dictionary<ObservedEventType, long> _appends = [];
        readonly Dictionary<(ObservedProducer Producer, ObservedEventType Type), long> _edges = [];
        readonly Dictionary<ObservedEventType, long> _unattributed = [];

        public long Events { get; private set; }

        public IEnumerable<ObservedAppend> Appends => _appends.Select(append => new ObservedAppend(append.Key, append.Value));

        public IEnumerable<ObservedEdge> Edges => _edges.Select(edge => new ObservedEdge(edge.Key.Producer, edge.Key.Type, edge.Value));

        public IEnumerable<UnattributedAppend> Unattributed => _unattributed.Select(append => new UnattributedAppend(append.Key, append.Value));

        public void Add(ObservedEventMetadata metadata)
        {
            var type = new ObservedEventType(metadata.EventTypeId, _generations.GetValueOrDefault(metadata.EventTypeId, 1u));
            Events++;
            _appends[type] = _appends.GetValueOrDefault(type) + 1;
            if (metadata.Producer is { } producer)
            {
                _edges[(producer, type)] = _edges.GetValueOrDefault((producer, type)) + 1;
            }
            else
            {
                _unattributed[type] = _unattributed.GetValueOrDefault(type) + 1;
            }
        }
    }
}
