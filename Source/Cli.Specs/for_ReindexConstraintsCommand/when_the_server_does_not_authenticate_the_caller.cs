// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints.Operator;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand;

public class when_the_server_does_not_authenticate_the_caller : given.a_reindex_context
{
    void Establish() =>
        _constraintIndexes
            .ReindexConstraints(Arg.Any<ReindexConstraintsRequest>(), Arg.Any<CallContext>())
            .Returns<Task<Cratis.Chronicle.Contracts.Commands.CommandResult<Guid>>>(_ => throw new RpcException(new Status(StatusCode.Unauthenticated, "no")));

    Task Because() => Run();

    [Fact] void should_return_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_start_a_second_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
}
