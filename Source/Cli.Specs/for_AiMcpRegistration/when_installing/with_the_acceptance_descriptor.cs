// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_the_acceptance_descriptor : given.a_playwright_corpus
{
    const string Entry = """{"type":"stdio","command":"npx","args":["-y","@playwright/mcp@0.0.83","--headless","--isolated"]}""";

    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"1.0","servers":[{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y","@playwright/mcp@0.0.83","--headless","--isolated"]}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_write_the_whole_claude_entry() => JsonNode.DeepEquals(Read(".mcp.json")["mcpServers"]!["playwright"], JsonNode.Parse(Entry)).ShouldBeTrue();
    [Fact] void should_write_the_whole_copilot_entry() => JsonNode.DeepEquals(Read(".vscode/mcp.json")["servers"]!["playwright"], JsonNode.Parse(Entry)).ShouldBeTrue();
    [Fact] void should_write_the_whole_cursor_entry() => JsonNode.DeepEquals(Read(".cursor/mcp.json")["mcpServers"]!["playwright"], JsonNode.Parse(Entry)).ShouldBeTrue();
    [Fact] void should_write_the_codex_table_without_env() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldNotContain("env");
    [Fact] void should_write_the_codex_command() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldContain("command = \"npx\"");
    [Fact] void should_write_the_opencode_entry_without_environment() => Read("opencode.json")["mcp"]!["playwright"]!.AsObject().ContainsKey("environment").ShouldBeFalse();
}
