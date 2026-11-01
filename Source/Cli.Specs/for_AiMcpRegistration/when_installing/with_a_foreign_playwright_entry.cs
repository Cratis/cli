// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_foreign_playwright_entry : given.a_playwright_corpus
{
    const string Original = """{"mcpServers":{"playwright":{"command":"my-browser"}}}""";

    void Establish() => Write(".mcp.json", Original);
    void Because() => _result = Install(force: true);

    [Fact] void should_report_the_member_conflict() => _result.Conflicts.Single().ShouldContain("user-owned MCP entry");
    [Fact] void should_preserve_the_foreign_entry() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
}
