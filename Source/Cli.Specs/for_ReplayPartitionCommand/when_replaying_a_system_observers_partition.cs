// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.given;
using ReplayPartitionContract = Cratis.Chronicle.Contracts.Observation.ReplayPartition;

namespace Cratis.Cli.for_ReplayPartitionCommand;

[Collection(CliSpecsCollection.Name)]
public class when_replaying_a_system_observers_partition : for_ObserverCommandSettings.given.observer_definitions
{
    IServices _services;
    PartitionCommandSettings _partitionSettings;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);
        _partitionSettings = new PartitionCommandSettings
        {
            EventStore = _settings.EventStore,
            Namespace = _settings.Namespace,
            ObserverId = _settings.ObserverId,
            Partition = "the-partition"
        };
    }

    Task Because() => new ReplayPartitionCommandForSpecs().Execute(_services, _partitionSettings);

    [Fact]
    void should_address_the_observers_sequence() => _observers.Received(1).ReplayPartition(
        Arg.Is<ReplayPartitionContract>(_ => _.EventSequenceId == "system" && _.ObserverId == "system-observer" && _.Partition == "the-partition"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());

    class ReplayPartitionCommandForSpecs : ReplayPartitionCommand
    {
        public Task<int> Execute(IServices services, PartitionCommandSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }
}
