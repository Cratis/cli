// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tomlyn;
using Tomlyn.Model;

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_playwright_selected : given.a_playwright_corpus
{
    const string Expected = "-y @playwright/mcp@0.0.83 --headless --isolated";

    void Because() => _result = Install();

    [Fact] void should_register_claude_with_the_exact_descriptor_args() => Args(Read(".mcp.json")["mcpServers"]!["playwright"]).ShouldEqual(Expected);
    [Fact] void should_register_claude_with_the_descriptor_command() => Read(".mcp.json")["mcpServers"]!["playwright"]!["command"]!.GetValue<string>().ShouldEqual("npx");
    [Fact] void should_register_copilot_with_the_exact_descriptor_args() => Args(Read(".vscode/mcp.json")["servers"]!["playwright"]).ShouldEqual(Expected);
    [Fact] void should_register_cursor_with_the_exact_descriptor_args() => Args(Read(".cursor/mcp.json")["mcpServers"]!["playwright"]).ShouldEqual(Expected);
    [Fact] void should_register_opencode_with_a_native_command_array() => string.Join(' ', Read("opencode.json")["mcp"]!["playwright"]!["command"]!.AsArray().Select(value => value!.GetValue<string>())).ShouldEqual($"npx {Expected}");
    [Fact] void should_register_codex_args_in_native_toml() => Args(JsonNodeOf(ReadToml("playwright"))).ShouldEqual(Expected);
    [Fact] void should_write_env_for_claude() => Read(".mcp.json")["mcpServers"]!["playwright"]!["env"]!["PLAYWRIGHT_BROWSERS_PATH"]!.GetValue<string>().ShouldEqual("0");
    [Fact] void should_write_env_for_opencode_as_environment() => Read("opencode.json")["mcp"]!["playwright"]!["environment"]!["PLAYWRIGHT_BROWSERS_PATH"]!.GetValue<string>().ShouldEqual("0");
    [Fact] void should_write_env_for_codex_as_a_table() => ((TomlTable)ReadToml("playwright")["env"])["PLAYWRIGHT_BROWSERS_PATH"].ShouldEqual("0");
    [Fact] void should_not_register_screenplay_for_an_unselected_profile() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("screenplay").ShouldBeFalse();
    [Fact] void should_write_env_for_copilot() => Read(".vscode/mcp.json")["servers"]!["playwright"]!["env"]!["PLAYWRIGHT_BROWSERS_PATH"]!.GetValue<string>().ShouldEqual("0");
    [Fact] void should_write_env_for_cursor() => Read(".cursor/mcp.json")["mcpServers"]!["playwright"]!["env"]!["PLAYWRIGHT_BROWSERS_PATH"]!.GetValue<string>().ShouldEqual("0");
    [Fact] void should_not_route_playwright_to_the_screenplay_only_pi_extension() => (AiCorpusSynchronizer.Status(_project).McpExtensions ?? []).ShouldNotContain("pi/playwright");
    [Fact] void should_report_playwright_unsupported_for_pi() => _result.UnsupportedMcpServers!.Single().ShouldContain("pi/playwright");
    [Fact] void should_report_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_report_no_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();

    TomlTable ReadToml(string id) => (TomlTable)((TomlTable)TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(ProjectFile(".codex/config.toml")))!["mcp_servers"])[id];
    static System.Text.Json.Nodes.JsonNode? JsonNodeOf(TomlTable table) => System.Text.Json.JsonSerializer.SerializeToNode(table);
}
