// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand;

[Collection(CliSpecsCollection.Name)]
public class when_pinning_a_login_after_logout : given.an_inactive_login
{
    int _exitCode;

    async Task Because() => _exitCode = await new DirectMcpInstallCommand().ExecuteAsync(null!, new() { Clients = "claude", NoTenant = true, DryRun = true, Output = "json-compact" }, CancellationToken.None);

    [Fact] void should_allow_the_explicitly_pinned_install() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Theory]
    [InlineData(null, true)]
    [InlineData("https://direct.example", false)]
    void should_allow_explicit_bridge_pins(string? url, bool noTenant) => DirectMcpRunner.Resolve(_direct, new(url, null, noTenant)).Credential.ShouldEqual(_direct.Credentials.Single());
}
