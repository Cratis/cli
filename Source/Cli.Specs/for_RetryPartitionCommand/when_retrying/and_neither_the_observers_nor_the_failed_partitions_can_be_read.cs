// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

public class and_neither_the_observers_nor_the_failed_partitions_can_be_read : given.a_retry_partition_command
{
    int _result;

    void Establish()
    {
        ObserversAreDenied();
        FailedPartitionsAreDenied();
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_it() => _observers.Received(1).RetryPartition(Arg.Any<RetryPartitionContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
