// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_observer_is_quarantined : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => Responds(ClearPartitionQuarantineOutcome.Cleared, PartitionRecoveryOutcome.ObserverQuarantined);

    async Task Because() => _result = await Execute();

    [Fact] void should_not_report_success() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_partition_was_cleared() => _error.ToString().ShouldContain("Quarantine cleared");
    [Fact] void should_suggest_clearing_the_observer_quarantine() => _error.ToString().ShouldContain("observers clear-quarantine the-observer");
}
