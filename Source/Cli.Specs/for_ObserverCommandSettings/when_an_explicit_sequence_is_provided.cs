// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ObserverCommandSettings;

public class when_an_explicit_sequence_is_provided : given.observer_definitions
{
    void Establish() => _settings.EventSequenceId = "custom-sequence";

    Task Because() => Resolve();

    [Fact] void should_preserve_the_explicit_sequence() => _result.ShouldEqual("custom-sequence");

    [Fact]
    void should_not_query_observers() => _observers.DidNotReceive().GetObservers(
        Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
