// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class in_the_project_scope : given.a_home_and_a_project
{
    void Because() => _plan = Install(DirectMcpScope.Project, _every);

    [Fact] void should_register_claude_in_the_project() => ReadJson(ProjectFile(".mcp.json"))["mcpServers"]!["cratis-direct"].ShouldNotBeNull();
    [Fact] void should_register_codex_in_the_project() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldContain("[mcp_servers.cratis-direct]");
    [Fact] void should_register_copilot_in_the_workspace() => ReadJson(ProjectFile(".vscode/mcp.json"))["servers"]!["cratis-direct"].ShouldNotBeNull();
    [Fact] void should_register_cursor_in_the_project() => ReadJson(ProjectFile(".cursor/mcp.json"))["mcpServers"]!["cratis-direct"].ShouldNotBeNull();
    [Fact] void should_register_opencode_in_the_project() => ReadJson(ProjectFile("opencode.json"))["mcp"]!["cratis-direct"].ShouldNotBeNull();
    [Fact] void should_record_ownership_in_the_project() => DirectMcpManifest.Read(_project).Servers.Count.ShouldEqual(5);
    [Fact] void should_not_touch_the_home_directory() => Directory.GetFileSystemEntries(_home).ShouldBeEmpty();
}
