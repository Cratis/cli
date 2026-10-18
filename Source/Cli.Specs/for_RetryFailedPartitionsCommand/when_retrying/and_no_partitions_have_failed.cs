// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RetryFailedPartitionsCommand.when_retrying;

public class and_no_partitions_have_failed : given.a_retry_failed_partitions_command
{
    int _result;

    void Establish() => FailedPartitions(0);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed_because_there_was_nothing_to_do() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_say_there_was_nothing_to_retry() => _output.ToString().ShouldContain("nothing to retry");
}
