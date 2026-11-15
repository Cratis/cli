// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_again : given.a_home_and_a_project
{
    void Establish() => Install(DirectMcpScope.User, ["pi"]);
    void Because() => _plan = Install(DirectMcpScope.User, ["pi"]);
    [Fact] void should_change_nothing() => _plan.Changes.ShouldBeEmpty();
    [Fact] void should_have_no_conflicts() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_use_the_default_path() => IsRegistered(HomeFile(".pi/agent/mcp.json"), "mcpServers").ShouldBeTrue();
}
