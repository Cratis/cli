// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_partition_is_not_quarantined : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => Responds(ClearPartitionQuarantineOutcome.NotQuarantined);

    async Task Because() => _result = await Execute();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_suggest_retrying() => _error.ToString().ShouldContain("observers retry-partition the-observer the-partition");
}
