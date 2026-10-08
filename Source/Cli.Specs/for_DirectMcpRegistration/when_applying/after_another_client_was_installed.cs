// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_another_client_was_installed : given.a_home_and_a_project
{
    Exception? _error;

    void Establish()
    {
        _plan = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["cursor"], DirectMcpClients.Arguments(Origin, null));
        Install(DirectMcpScope.User, ["claude"]);
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));

    [Fact] void should_reject_the_stale_plan() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_leave_the_second_client_untouched() => File.Exists(HomeFile(".cursor/mcp.json")).ShouldBeFalse();
    [Fact] void should_keep_owning_the_first_client() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("claude");
    [Fact] void should_be_recoverable_by_replanning()
    {
        Install(DirectMcpScope.User, ["cursor"]);
        DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("claude", "cursor");
        Uninstall(DirectMcpScope.User);
        IsRegistered(HomeFile(".claude.json"), "mcpServers").ShouldBeFalse();
        IsRegistered(HomeFile(".cursor/mcp.json"), "mcpServers").ShouldBeFalse();
    }
}
