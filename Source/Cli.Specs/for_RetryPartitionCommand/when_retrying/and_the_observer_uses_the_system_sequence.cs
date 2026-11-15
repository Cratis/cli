// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

public class and_the_observer_uses_the_system_sequence : given.a_retry_partition_command
{
    void Establish() => _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(
    [
        new ObserverInformation { Id = "the-observer", EventSequenceId = "system" }
    ]);

    Task Because() => Execute();

    [Fact]
    void should_address_the_observers_sequence() => _observers.Received(1).RetryPartition(
        Arg.Is<RetryPartitionContract>(_ => _.EventSequenceId == "system" && _.ObserverId == "the-observer"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
