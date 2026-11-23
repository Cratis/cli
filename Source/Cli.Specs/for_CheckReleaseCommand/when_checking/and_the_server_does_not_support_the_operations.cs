// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_server_does_not_support_the_operations : given.a_release_target
{
    async Task Because()
    {
        var unimplemented = new RpcException(new Status(StatusCode.Unimplemented, "no"));
        _services.Server.GetVersionInfo().Returns(Task.FromException<ServerVersionInfo>(unimplemented));
        _services.Connections.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(Task.FromException<CompatibilityResponse>(unimplemented));
        _services.EventTypes.CheckEventTypeRegistrations(Arg.Any<CheckEventTypeRegistrationsRequest>(), Arg.Any<CallContext>())
            .Returns(Task.FromException<CommandResult<CheckEventTypeRegistrationsResponse>>(unimplemented));
        await Check();
    }

    [Fact] void should_report_every_check_as_unknown() => _result.Checks.All(_ => _.Verdict == CompatibilityVerdict.Unknown).ShouldBeTrue();
    [Fact] void should_not_be_compatible() => _result.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
    [Fact] void should_always_say_what_to_do() => _result.Checks.All(_ => _.Action.Length > 0).ShouldBeTrue();
    [Fact] void should_exit_with_server_error() => _result.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
