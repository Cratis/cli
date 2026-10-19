// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;

namespace Cratis.Cli.for_NamespaceObservationSnapshot.given;

public class a_namespace : Specification
{
    protected static readonly NamespaceTarget _target = new("Library", "Default", "event-log");
    protected static readonly ObservedProducer _register = new(ObservedProducerKind.Command, "Register");
    protected INamespaceObservations _observations = null!;
    protected List<ObservedEventMetadata> _events = [];
    protected List<ObservedEventType> _registered = [new("Registered", 1)];
    protected List<IReadOnlyList<ulong>> _pages = [];
    protected ulong? _tail;
    protected NamespaceObservationScan _scan = new(null, null, NamespaceObservationScan.DefaultMaxEvents);
    protected ObservationSnapshot _snapshot = null!;

    void Establish()
    {
        _observations = Substitute.For<INamespaceObservations>();
        _observations.Tail(_target.EventStore, _target.Namespace, _target.EventSequenceId, Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(_tail));
        _observations.RegisteredEventTypes(_target.EventStore, Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IReadOnlyList<ObservedEventType>>(_registered));
        _observations.Consumers(_target.EventStore, _target.Namespace, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ObservedConsumer>>([]));
        _observations.Metadata(_target.EventStore, _target.Namespace, _target.EventSequenceId, Arg.Any<IReadOnlyList<ulong>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var numbers = call.ArgAt<IReadOnlyList<ulong>>(3);
            _pages.Add(numbers);
            return Task.FromResult<IReadOnlyList<ObservedEventMetadata>>([.. _events.Where(metadata => numbers.Contains(metadata.SequenceNumber))]);
        });
    }

    protected void Appended(int count, ObservedProducer? producer = null, string type = "Registered")
    {
        for (var index = 0; index < count; index++)
        {
            _events.Add(new((ulong)_events.Count, type, producer));
        }
    }

    protected async Task Build() => _snapshot = await NamespaceObservationSnapshot.Build(_observations, _target, _scan, DateTimeOffset.UnixEpoch, CancellationToken.None);

    protected long Appends(string type) => _snapshot.Appends.Where(append => append.Type.TypeId == type).Sum(append => append.Count);
}
