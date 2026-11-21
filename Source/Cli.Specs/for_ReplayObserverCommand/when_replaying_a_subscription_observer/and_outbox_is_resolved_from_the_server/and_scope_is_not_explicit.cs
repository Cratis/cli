// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ReplayObserverCommand.when_replaying_a_subscription_observer.and_outbox_is_resolved_from_the_server;

[Collection(CliSpecsCollection.Name)]
public class and_scope_is_not_explicit : given.a_replay_observer_command
{
    void Establish()
    {
        _settings.ObserverId = ResolvedSubscriptionObserverId;
        _settings.EventStore = null;
        _settings.Yes = true;
    }

    async Task Because() => _result = await _command.ExecuteConnected(_services, _settings);

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_replay_the_observer() => _observers.DidNotReceive().Replay(Arg.Any<Replay>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
