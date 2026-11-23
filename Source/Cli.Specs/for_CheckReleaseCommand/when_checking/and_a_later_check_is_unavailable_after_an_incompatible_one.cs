// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;
using Grpc.Core;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_a_later_check_is_unavailable_after_an_incompatible_one : given.a_release_target
{
    async Task Because()
    {
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "19.100.0", ProtocolVersion = "1" });
        _services.Connections.CheckCompatibility(Arg.Any<CompatibilityRequest>())
            .Returns(Task.FromException<CompatibilityResponse>(new RpcException(new Status(StatusCode.Unavailable, "secret-host:1234"))));
        _services.EventTypes.CheckEventTypeRegistrations(Arg.Any<Cratis.Chronicle.Contracts.EventTypes.CheckEventTypeRegistrationsRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(Task.FromException<Cratis.Chronicle.Contracts.Commands.CommandResult<Cratis.Chronicle.Contracts.EventTypes.CheckEventTypeRegistrationsResponse>>(new InvalidOperationException("secret-host:1234")));
        await Check();
    }

    [Fact] void should_keep_the_incompatible_finding() => Kernel.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_report_the_unavailable_check_as_unknown() => Contracts.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
    [Fact] void should_report_an_unexpected_failure_as_unknown() => Schemas.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
    [Fact] void should_be_incompatible_overall() => _result.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_exit_with_validation_error() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_leak_the_raw_payload() => _result.Checks.Any(_ => _.Actual.Contains("secret-host")).ShouldBeFalse();
}
