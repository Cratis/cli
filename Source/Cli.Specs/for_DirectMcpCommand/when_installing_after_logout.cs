// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand;

[Collection(CliSpecsCollection.Name)]
public class when_installing_after_logout : given.an_inactive_login
{
    int _exitCode;

    async Task Because() => _exitCode = await new DirectMcpInstallCommand().ExecuteAsync(null!, new() { Clients = "claude", DryRun = true, Output = "json-compact" }, CancellationToken.None);

    [Fact] void should_refuse_an_unpinned_install() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_create_client_configuration() => File.Exists(Path.Combine(_tempConfigHome, ".claude.json")).ShouldBeFalse();
    [Fact] void should_keep_the_inactive_no_tenant_login() => _direct.Credentials.Single().Tenant.ShouldBeNull();
}
