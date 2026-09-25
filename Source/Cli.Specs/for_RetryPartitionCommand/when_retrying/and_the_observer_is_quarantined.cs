// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

/// <summary>
/// A quarantined observer has recovery paused by design, so the retry goes nowhere. This used to print "Retry started"
/// and exit zero, sending the operator off to watch progress that was never coming - which is how a production
/// recovery came down to hand-editing MongoDB.
/// </summary>
public class and_the_observer_is_quarantined : given.a_retry_partition_command
{
    int _result;

    void Establish() => Recovery(PartitionRecoveryOutcome.ObserverQuarantined);

    async Task Because() => _result = await Execute();

    [Fact] void should_not_report_success() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
