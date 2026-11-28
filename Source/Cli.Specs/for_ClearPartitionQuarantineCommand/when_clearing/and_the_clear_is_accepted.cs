// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_clear_is_accepted : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => Responds(ClearPartitionQuarantineOutcome.Accepted, PartitionRecoveryOutcome.Accepted);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_say_it_was_accepted() => _output.ToString().ShouldContain("accepted");
}
