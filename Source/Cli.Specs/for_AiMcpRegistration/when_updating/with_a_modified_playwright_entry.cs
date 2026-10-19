// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_modified_playwright_entry : given.a_playwright_corpus
{
    void Establish()
    {
        Install();
        Write(".mcp.json", """{"mcpServers":{"playwright":{"command":"changed"}}}""");
    }

    void Because() => _result = Install();

    [Fact] void should_report_the_modified_owned_conflict() => _result.Conflicts.Single().ShouldContain("modified owned MCP entry");
    [Fact] void should_preserve_the_changed_entry() => Read(".mcp.json")["mcpServers"]!["playwright"]!["command"]!.GetValue<string>().ShouldEqual("changed");
}
