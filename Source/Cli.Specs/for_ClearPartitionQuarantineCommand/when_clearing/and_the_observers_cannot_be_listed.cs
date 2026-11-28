// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_observers_cannot_be_listed : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => ObserversAreDenied();

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_it() => _observers.Received(1).ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact]
    void should_address_the_default_sequence() => _observers.Received(1).ClearPartitionQuarantine(
        Arg.Is<ClearPartitionQuarantineContract>(_ => _.EventSequenceId == "event-log"),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
