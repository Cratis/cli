// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_it_is_unknown_whether_the_partition_is_quarantined : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => FailedPartitions(new FailedPartition { ObserverId = "the-observer", Partition = "the-partition", IsQuarantined = null });

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_the_clear() => _observers.Received(1).ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
