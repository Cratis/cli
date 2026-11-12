// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ReplayObserverCommand;

[Collection(CliSpecsCollection.Name)]
public class when_replaying_a_system_observer : for_ObserverCommandSettings.given.observer_definitions
{
    IServices _services;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);
    }

    Task Because() => new ReplayObserverCommandForSpecs().Execute(_services, _settings);

    [Fact]
    void should_address_the_observers_sequence() => _observers.Received(1).Replay(
        Arg.Is<Replay>(_ => _.EventSequenceId == "system" && _.ObserverId == "system-observer"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());

    class ReplayObserverCommandForSpecs : ReplayObserverCommand
    {
        public Task<int> Execute(IServices services, ObserverCommandSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }
}
