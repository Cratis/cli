// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_observer_uses_the_system_sequence : given.a_clear_partition_quarantine_command
{
    void Establish() => _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(
    [
        new ObserverInformation { Id = "the-observer", EventSequenceId = "system" }
    ]);

    Task Because() => Execute();

    [Fact]
    void should_address_the_observers_sequence() => _observers.Received(1).ClearPartitionQuarantine(
        Arg.Is<ClearPartitionQuarantineContract>(_ => _.EventSequenceId == "system" && _.ObserverId == "the-observer"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
