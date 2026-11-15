// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_with_a_foreign_entry : given.a_home_and_a_project
{
    const string Mine = "{\"mcpServers\":{\"cratis-direct\":{\"command\":\"mine\"}}}";
    void Establish() => Write(HomeFile(".pi/agent/mcp.json"), Mine);
    void Because() => _plan = Install(DirectMcpScope.User, ["pi"]);
    [Fact] void should_refuse() => _plan.Conflicts.ShouldNotBeEmpty();
    [Fact] void should_preserve_the_file() => File.ReadAllText(HomeFile(".pi/agent/mcp.json")).ShouldEqual(Mine);
}
