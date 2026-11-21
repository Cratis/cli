// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ReplayObserverCommand.when_replaying_a_subscription_observer;

[Collection(CliSpecsCollection.Name)]
public class and_confirmation_is_given : given.a_replay_observer_command
{
    void Establish()
    {
        _settings.ObserverId = SubscriptionObserverId;
        _settings.EventSequenceId = "outbox";
        _settings.Yes = true;
    }

    async Task Because() => _result = await _command.ExecuteConnected(_services, _settings);

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_replay_the_observer_once_in_the_producer_scope() => _observers.Received(1).Replay(
        Arg.Is<Replay>(_ => _.ObserverId == SubscriptionObserverId && _.EventSequenceId == "outbox" && _.EventStore == "producer-store" && _.Namespace == "producer-namespace"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
