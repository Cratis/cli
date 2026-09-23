// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_matching_foreign_entry : given.a_screenplay_corpus
{
    const string Original = """{"mcpServers":{"screenplay":{"type":"stdio","command":"cratis","args":["screenplay","mcp","--project-root-env","CLAUDE_PROJECT_DIR"]}}}""";

    void Establish() => Write(".mcp.json", Original);
    void Because() => _result = Install();

    [Fact] void should_not_adopt_a_foreign_entry_just_because_its_value_matches() => _result.Conflicts.Single().ShouldContain("user-owned MCP entry");
    [Fact] void should_leave_the_foreign_bytes_intact() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
}
