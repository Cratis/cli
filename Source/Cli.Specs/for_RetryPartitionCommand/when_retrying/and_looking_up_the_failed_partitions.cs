// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

public class and_looking_up_the_failed_partitions : given.a_retry_partition_command
{
    async Task Because() => await Execute();

    [Fact]
    void should_ask_for_the_failed_partitions_of_the_observer() =>
        _services.FailedPartitions.Received(1).GetFailedPartitions(Arg.Is<GetFailedPartitionsRequest>(_ =>
            _.EventStore == "the-event-store" && _.Namespace == "the-namespace" && _.ObserverId == "the-observer"));
}
