// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_a_user_owned_entry : given.a_home_and_a_project
{
    const string Mine = "{\"mcpServers\":{\"cratis-direct\":{\"type\":\"http\",\"url\":\"https://cratis.direct/mcp\"}}}";

    void Establish() => Write(HomeFile(".claude.json"), Mine);

    void Because() => _plan = Install(DirectMcpScope.User, ["claude", "cursor"]);

    [Fact] void should_report_the_conflict() => _plan.Conflicts.ShouldContainOnly(".claude.json:mcpServers.cratis-direct (user-owned MCP entry)");
    [Fact] void should_leave_the_entry_alone() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual(Mine);
    [Fact] void should_not_register_other_clients_either() => File.Exists(HomeFile(".cursor/mcp.json")).ShouldBeFalse();
    [Fact] void should_not_claim_ownership() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
