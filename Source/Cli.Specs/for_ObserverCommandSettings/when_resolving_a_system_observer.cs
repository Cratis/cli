// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ObserverCommandSettings;

public class when_resolving_a_system_observer : given.observer_definitions
{
    Task Because() => Resolve();

    [Fact] void should_use_the_observers_own_sequence() => _result.ShouldEqual("system");

    [Fact]
    void should_query_the_addressed_store_and_namespace() => _observers.Received(1).GetObservers(
        Arg.Is<AllObserversRequest>(_ => _.EventStore == "the-event-store" && _.Namespace == "the-namespace"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
