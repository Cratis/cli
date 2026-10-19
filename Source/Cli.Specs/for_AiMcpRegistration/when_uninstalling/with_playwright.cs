// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_uninstalling;

public class with_playwright : given.a_playwright_corpus
{
    void Establish()
    {
        Install();
        var host = Read(".cursor/mcp.json");
        host["mcpServers"]!["mine"] = System.Text.Json.Nodes.JsonNode.Parse("""{"command":"mine"}""");
        Write(".cursor/mcp.json", host.ToJsonString());
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project, force: false);

    [Fact] void should_report_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_it_from_cursor() => Read(".cursor/mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeFalse();
    [Fact] void should_keep_user_owned_entries() => Read(".cursor/mcp.json")["mcpServers"]!.AsObject().ContainsKey("mine").ShouldBeTrue();
    [Fact] void should_remove_it_from_codex() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldNotContain("playwright");
    [Fact] void should_remove_it_from_opencode() => Read("opencode.json")["mcp"]!.AsObject().ContainsKey("playwright").ShouldBeFalse();
}
