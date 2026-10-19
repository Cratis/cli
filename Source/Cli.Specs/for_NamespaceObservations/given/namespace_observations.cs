// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;

namespace Cratis.Cli.for_NamespaceObservations.given;

public class namespace_observations : Specification
{
    protected IServices _services = null!;
    protected IEventStores _eventStores = null!;
    protected IObservers _observers = null!;
    protected NamespaceObservations _observations = null!;
    protected Exception? _failure;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _eventStores = Substitute.For<IEventStores>();
        _observers = Substitute.For<IObservers>();
        _services.EventStores.Returns(_eventStores);
        _services.Observers.Returns(_observers);
        _observations = new(_services);
    }

    protected void EventStoresReturn(QueryResult<IEnumerable<EventStoreNamesResponse>> result) =>
        _eventStores.AllEventStores(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Task.FromResult(result));

    protected void ObserversThrow(StatusCode code) =>
        _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns<Task<IEnumerable<ObserverInformation>>>(_ => throw new RpcException(new Status(code, "refused")));
}
