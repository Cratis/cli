// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class with_a_modified_registration : given.a_home_and_a_project
{
    const string Edited = "{\"mcpServers\":{\"cratis-direct\":{\"type\":\"stdio\",\"command\":\"/opt/cratis\",\"args\":[\"direct\",\"mcp\"]}}}";

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        File.WriteAllText(HomeFile(".claude.json"), Edited);
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [Fact] void should_report_the_conflict() => _plan.Conflicts.ShouldContainOnly(".claude.json:mcpServers.cratis-direct (modified owned MCP entry)");
    [Fact] void should_leave_the_entry_alone() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual(Edited);
    [Fact] void should_keep_the_ownership_record() => DirectMcpManifest.Read(_home).Servers.Count.ShouldEqual(1);
}
