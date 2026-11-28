// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Events.Constraints.Operator;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand;

public class when_the_caller_is_not_an_administrator : given.a_reindex_context
{
    void Establish() =>
        _constraintIndexes
            .ReindexConstraints(Arg.Any<ReindexConstraintsRequest>(), Arg.Any<CallContext>())
            .Returns(new CommandResult<Guid> { IsAuthorized = false });

    Task Because() => Run();

    [Fact] void should_return_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_say_an_administrator_is_needed() => _error.ToString().ShouldContain("cratis chronicle login");
}
