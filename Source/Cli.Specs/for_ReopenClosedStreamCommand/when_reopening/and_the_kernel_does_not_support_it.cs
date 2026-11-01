// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Grpc.Core;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_reopening;

public class and_the_kernel_does_not_support_it : given.a_reopen_closed_stream_command
{
    int _result;

    void Establish() => _sequences.ReopenStreamScope(Arg.Any<ReopenStreamScopeRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
        .Returns<CommandResult<ReopenStreamScopeResponse>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "not implemented")));

    async Task Because() => _result = await Execute();

    [Fact] void should_fail() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_minimum_chronicle_version() => _error.ToString().ShouldContain("requires Chronicle 19.39.0 or later");
}
