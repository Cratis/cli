// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_contracts_are_incompatible : given.a_release_target
{
    async Task Because()
    {
        _services.Connections.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(new CompatibilityResponse
        {
            IsCompatible = false,
            ServerVersion = "19.181.3",
            ServerProtocolVersion = "1",
            Incompatibilities = ["Method Foo was removed."]
        });
        await Check();
    }

    [Fact] void should_report_the_contracts_as_incompatible() => Contracts.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_include_the_incompatibility() => Contracts.Actual.ShouldContain("Method Foo was removed.");
    [Fact] void should_send_the_releases_own_descriptor_set() => _services.Connections.Received(1).CheckCompatibility(Arg.Is<CompatibilityRequest>(_ => _.DescriptorSet.SequenceEqual(new byte[] { 1, 2, 3 }) && _.ClientVersion == "19.181.3"));
}
