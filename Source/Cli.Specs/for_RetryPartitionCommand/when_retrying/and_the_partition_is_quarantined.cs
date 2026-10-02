// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.given;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

[Collection(CliSpecsCollection.Name)]
public class and_the_partition_is_quarantined : given.a_retry_partition_command
{
    int _result;
    StringWriter _error;
    TextWriter _previousError;

    void Establish()
    {
        Recovery(PartitionRecoveryOutcome.PartitionQuarantined);
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because() => _result = await Execute();

    void Destroy() => Console.SetError(_previousError);

    [Fact] void should_not_report_success() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_suggest_clearing_the_partition_quarantine() =>
        _error.ToString().ShouldContain("observers clear-partition-quarantine the-observer the-partition");
}
