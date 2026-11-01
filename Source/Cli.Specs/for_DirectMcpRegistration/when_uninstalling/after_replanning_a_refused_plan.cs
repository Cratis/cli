// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_replanning_a_refused_plan : given.a_home_and_a_project
{
    void Establish()
    {
        _plan = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["cursor"], DirectMcpClients.Arguments(Origin, null));
        Install(DirectMcpScope.User, ["claude"]);
        Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));
        Install(DirectMcpScope.User, ["cursor"]);
    }

    void Because() => Uninstall(DirectMcpScope.User);

    [Fact] void should_remove_the_first_client() => IsRegistered(HomeFile(".claude.json"), "mcpServers").ShouldBeFalse();
    [Fact] void should_remove_the_replanned_client() => IsRegistered(HomeFile(".cursor/mcp.json"), "mcpServers").ShouldBeFalse();
}
