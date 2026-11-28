// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_several_namespaces_match_only_by_letter_case : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish()
    {
        _settings.Namespace = "The-Namespace";
        FailedPartitions();
        Namespaces("the-namespace", "THE-NAMESPACE");
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_it() => _observers.Received(1).ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
