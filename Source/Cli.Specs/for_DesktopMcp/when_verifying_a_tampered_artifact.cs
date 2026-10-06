// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_verifying_a_tampered_artifact : given.a_personal_marketplace
{
    async Task Because() => _error = await Catch.Exception(() => DesktopMcpArtifacts.Verify(_artifact, new string('0', 64)));

    [Fact] void should_reject_the_bytes_before_host_installation() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
}
