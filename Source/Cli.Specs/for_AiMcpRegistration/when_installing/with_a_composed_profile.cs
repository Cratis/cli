// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_composed_profile : given.a_screenplay_corpus
{
    void Because() => _result = Install();

    [Fact] void should_register_claude_with_its_injected_project_environment() => Read(".mcp.json")["mcpServers"]!["screenplay"]!["args"]![3]!.GetValue<string>().ShouldEqual("CLAUDE_PROJECT_DIR");
    [Fact] void should_register_copilot_using_the_workspace_variable() => Read(".vscode/mcp.json")["servers"]!["screenplay"]!["args"]![3]!.GetValue<string>().ShouldEqual("${workspaceFolder}");
    [Fact] void should_register_cursor_using_the_workspace_variable() => Read(".cursor/mcp.json")["mcpServers"]!["screenplay"]!["args"]![3]!.GetValue<string>().ShouldEqual("${workspaceFolder}");
    [Fact] void should_register_opencode_with_its_native_command_array() => Read("opencode.json")["mcp"]!["screenplay"]!["command"]![0]!.GetValue<string>().ShouldEqual("cratis");
    [Fact] void should_create_the_conventional_empty_model_directory() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeTrue();
    [Fact] void should_not_create_source_files() => Directory.GetFiles(ProjectFile(".cratis/screenplay")).ShouldBeEmpty();
    [Fact] void should_carry_the_descriptor_through_filtered_selection() => File.Exists(ProjectFile(".cratis/ai/mcp-servers.json")).ShouldBeTrue();
    [Fact] void should_configure_every_selected_harness() => _result.UnsupportedMcpServers!.ShouldBeEmpty();
    [Fact] void should_report_the_native_pi_extension() => AiCorpusSynchronizer.Status(_project).McpExtensions!.ShouldContain("pi/screenplay");
    [Fact] void should_carry_the_profile_catalog_for_the_native_extension() => File.Exists(ProjectFile(".cratis/ai/profile-catalog.json")).ShouldBeTrue();
    [Fact] void should_register_codex_in_native_toml() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldContain("[mcp_servers.screenplay]");
    [Fact] void should_report_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_never_symlink_a_host_configuration() => (new FileInfo(ProjectFile(".mcp.json")).LinkTarget is null).ShouldBeTrue();
}
