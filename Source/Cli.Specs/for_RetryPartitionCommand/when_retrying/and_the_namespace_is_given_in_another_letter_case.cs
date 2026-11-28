// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

public class and_the_namespace_is_given_in_another_letter_case : given.a_retry_partition_command
{
    int _result;

    void Establish()
    {
        _settings.Namespace = "The-Namespace";
        FailedPartitions();
        Namespaces("the-namespace", "other");
        FailedPartitionsInNamespace("the-namespace", new FailedPartition { ObserverId = "the-observer", Partition = "the-partition", IsQuarantined = true });
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_to_the_server() => _observers.Received(1).RetryPartition(Arg.Any<RetryPartitionContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_the_server_outcome() => _result.ShouldEqual(ExitCodes.Success);
}
