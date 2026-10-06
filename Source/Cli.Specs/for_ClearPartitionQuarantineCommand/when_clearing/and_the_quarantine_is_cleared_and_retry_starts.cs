// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_quarantine_is_cleared_and_retry_starts : given.a_clear_partition_quarantine_command
{
    int _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_request_an_immediate_retry() => _observers.Received(1).ClearPartitionQuarantine(Arg.Is<ClearPartitionQuarantineContract>(_ => _.RetryImmediately && _.Partition == "the-partition" && _.ObserverId == "the-observer"), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_say_the_retry_started() => _output.ToString().ShouldContain("retry started");
}
