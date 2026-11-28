// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

public class and_the_failed_partitions_cannot_be_read : given.a_retry_partition_command
{
    int _result;

    void Establish()
    {
        FailedPartitionsAreDenied();
        Recovery(PartitionRecoveryOutcome.Started);
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_the_retry() => _observers.Received(1).RetryPartition(Arg.Any<RetryPartitionContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_the_server_outcome() => _result.ShouldEqual(ExitCodes.Success);
}
