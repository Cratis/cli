// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints.Operator;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand;

public class when_the_server_denies_access : given.a_reindex_context
{
    void Establish() =>
        _constraintIndexes
            .ReindexConstraints(Arg.Any<ReindexConstraintsRequest>(), Arg.Any<CallContext>())
            .Returns<Task<Cratis.Chronicle.Contracts.Commands.CommandResult<Guid>>>(_ => throw new RpcException(new Status(StatusCode.PermissionDenied, "denied")));

    Task Because() => Run();

    [Fact] void should_return_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_point_at_login() => _error.ToString().ShouldContain("cratis chronicle login");
}
