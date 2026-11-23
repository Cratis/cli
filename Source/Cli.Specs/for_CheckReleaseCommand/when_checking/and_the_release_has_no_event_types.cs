// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Cli.Commands.Chronicle.CheckRelease;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_release_has_no_event_types : given.a_release_target
{
    async Task Because()
    {
        _release = _release with { EventTypes = [] };
        await Check();
    }

    [Fact] void should_pass_the_schema_check() => Schemas.Verdict.ShouldEqual(CompatibilityVerdict.Compatible);
    [Fact] void should_not_call_the_server() => _services.EventTypes.DidNotReceiveWithAnyArgs().CheckEventTypeRegistrations(default!, default);
}
