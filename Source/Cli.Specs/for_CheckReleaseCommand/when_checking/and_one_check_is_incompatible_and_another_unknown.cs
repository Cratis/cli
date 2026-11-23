// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Cli.Commands.Chronicle.CheckRelease;
using Grpc.Core;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_one_check_is_incompatible_and_another_unknown : given.a_release_target
{
    async Task Because()
    {
        _services.Connections.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(Task.FromException<CompatibilityResponse>(new RpcException(new Status(StatusCode.Unimplemented, "no"))));
        _schemaResult.Response = new CheckEventTypeRegistrationsResponse
        {
            Results = [new EventTypeCheckResult { EventType = "BookAdded", Generation = 1, Accepted = false, Reason = "no" }]
        };
        await Check();
    }

    [Fact] void should_let_incompatible_outrank_unknown() => _result.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_keep_the_unknown_check_visible() => Contracts.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
}
