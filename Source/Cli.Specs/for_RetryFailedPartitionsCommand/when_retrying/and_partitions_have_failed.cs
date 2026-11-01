// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RetryFailedPartitionsCommand.when_retrying;

public class and_partitions_have_failed : given.a_retry_failed_partitions_command
{
    int _result;

    void Establish() => FailedPartitions(3);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_ask_the_kernel_for_the_observer() => _observers.Received(1).RetryFailedPartitions(Arg.Is<Cratis.Chronicle.Contracts.Observation.RetryFailedPartitions>(_ => _.ObserverId == "the-observer" && _.EventStore == "the-event-store" && _.Namespace == "the-namespace" && _.EventSequenceId == "event-log"), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_how_many_partitions_were_failed() => _output.ToString().ShouldContain("3");
}
