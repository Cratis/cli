// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_failed_partitions_cannot_be_read : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish()
    {
        FailedPartitionsAreDenied();
        Responds(ClearPartitionQuarantineOutcome.NotFound);
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_the_clear() => _observers.Received(1).ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_the_server_outcome() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_partition_was_not_among_the_failures() => _error.ToString().ShouldContain("is not among the failed partitions");
}
