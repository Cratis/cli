// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

[Collection(CliSpecsCollection.Name)]
public class and_the_partition_is_not_among_the_failed_partitions : given.a_retry_partition_command
{
    int _result;
    StringWriter _error;
    TextWriter _previousError;

    void Establish()
    {
        FailedPartitions(new FailedPartition { ObserverId = "the-observer", Partition = "another-partition" });
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because() => _result = await Execute();

    void Destroy() => Console.SetError(_previousError);

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_partition_was_not_among_the_failures() => _error.ToString().ShouldContain("is not among the failed partitions");
    [Fact] void should_not_submit_the_retry() => _observers.DidNotReceive().RetryPartition(Arg.Any<RetryPartitionContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
