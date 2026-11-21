// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ReplayObserverCommand;

[Collection(CliSpecsCollection.Name)]
public class when_replaying_an_ordinary_observer : given.a_replay_observer_command
{
    string? _prompt;

    void Establish()
    {
        _settings.ObserverId = OrdinaryObserverId;
        _settings.EventStore = null;
        _settings.Namespace = null;
    }

    async Task Because()
    {
        _prompt = _command.Prompt(_settings);
        _result = await _command.ExecuteConnected(_services, _settings);
    }

    [Fact] void should_keep_the_ordinary_confirmation_prompt() => _prompt.ShouldEqual($"Are you sure you want to replay observer '{OrdinaryObserverId}'?");
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_replay_the_observer_once_in_the_defaulted_scope() => _observers.Received(1).Replay(
        Arg.Is<Replay>(_ => _.ObserverId == OrdinaryObserverId && _.EventSequenceId == "event-log" && _.EventStore == "default" && _.Namespace == "Default"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
