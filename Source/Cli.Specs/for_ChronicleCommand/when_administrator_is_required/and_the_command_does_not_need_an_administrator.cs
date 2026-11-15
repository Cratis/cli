// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_command_does_not_need_an_administrator : given.an_administrative_command_context
{
    int _result;

    void Establish() => _jobs.DeleteJob(Arg.Any<Chronicle.Contracts.Jobs.DeleteJobRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
        .Returns(new CommandResult { IsAuthorized = false, AuthorizationFailureReason = "Deletion is not authorized" });

    async Task Because() => _result = await DeleteJob();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_surface_the_reason() => _error.ToString().ShouldContain("Deletion is not authorized");
    [Fact] void should_not_mention_the_login() => _error.ToString().ShouldNotContain("login");
}
