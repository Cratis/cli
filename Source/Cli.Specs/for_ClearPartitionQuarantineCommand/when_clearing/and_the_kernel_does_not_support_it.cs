// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_kernel_does_not_support_it : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish() =>
        _observers
            .ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns<ClearPartitionQuarantineResponse>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "not implemented")));

    async Task Because() => _result = await Execute();

    [Fact] void should_fail() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_minimum_chronicle_version() => _error.ToString().ShouldContain("19.29.0");
}
