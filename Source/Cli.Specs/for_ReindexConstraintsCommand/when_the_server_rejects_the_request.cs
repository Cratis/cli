// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Events.Constraints.Operator;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand;

public class when_the_server_rejects_the_request : given.a_reindex_context
{
    void Establish() =>
        _constraintIndexes
            .ReindexConstraints(Arg.Any<ReindexConstraintsRequest>(), Arg.Any<CallContext>())
            .Returns(new CommandResult<Guid> { ExceptionMessages = ["Unknown constraint 'Nope'"] });

    Task Because() => Run("Nope");

    [Fact] void should_return_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_report_the_server_message() => _error.ToString().ShouldContain("Unknown constraint");
    [Fact] void should_not_announce_a_job() => _output.ToString().ShouldNotContain(_jobId.ToString());
}
