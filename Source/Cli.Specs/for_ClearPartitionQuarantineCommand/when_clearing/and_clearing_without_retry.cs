// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_clearing_without_retry : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() => _settings.NoRetry = true;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_request_a_retry() => _observers.Received(1).ClearPartitionQuarantine(Arg.Is<ClearPartitionQuarantineContract>(_ => !_.RetryImmediately), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_say_no_retry_was_started() => _output.ToString().ShouldContain("no retry was started");
}
