// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_partition_is_not_among_the_failed_partitions : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => FailedPartitions(new FailedPartition { ObserverId = "the-observer", Partition = "another-partition", IsQuarantined = true });

    async Task Because() => _result = await Execute();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_partition_was_not_among_the_failures() => _error.ToString().ShouldContain("is not among the failed partitions");
    [Fact] void should_not_submit_the_clear() => _observers.DidNotReceive().ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
