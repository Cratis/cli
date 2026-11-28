// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

public class and_the_retry_is_accepted : given.a_retry_partition_command
{
    int _result;

    void Establish() => Recovery(PartitionRecoveryOutcome.Accepted);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
