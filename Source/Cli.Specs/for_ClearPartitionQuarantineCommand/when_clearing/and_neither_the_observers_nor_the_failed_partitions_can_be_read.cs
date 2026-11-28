// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_neither_the_observers_nor_the_failed_partitions_can_be_read : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish()
    {
        ObserversAreDenied();
        FailedPartitionsAreDenied();
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_it() => _observers.Received(1).ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
