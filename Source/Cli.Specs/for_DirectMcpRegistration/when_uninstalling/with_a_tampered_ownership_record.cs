// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class with_a_tampered_ownership_record : given.a_home_and_a_project
{
    const string Profile = "{\"mcpServers\":{\"cratis-direct\":{\"type\":\"stdio\",\"command\":\"cratis\",\"args\":[\"direct\",\"mcp\"]}}}";
    Exception _error;

    void Establish()
    {
        Write(HomeFile(".config/other.json"), Profile);
        Write(HomeFile(DirectMcpManifest.RelativePath), "{\"Servers\":[{\"Harness\":\"claude\",\"Path\":\".config/other.json\",\"Collection\":\"mcpServers\",\"Id\":\"cratis-direct\",\"Installed\":{\"type\":\"stdio\",\"command\":\"cratis\",\"args\":[\"direct\",\"mcp\"]}}]}");
    }

    void Because() => _error = Catch.Exception(() => Uninstall(DirectMcpScope.User));

    [Fact] void should_refuse_the_record() => _error.Message.ShouldContain("Invalid Direct MCP ownership record");
    [Fact] void should_leave_the_file_it_points_at() => File.ReadAllText(HomeFile(".config/other.json")).ShouldEqual(Profile);
}
