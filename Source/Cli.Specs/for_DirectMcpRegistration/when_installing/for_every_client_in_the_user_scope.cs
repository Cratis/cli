// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class for_every_client_in_the_user_scope : given.a_home_and_a_project
{
    void Establish() => Write(HomeFile(".claude.json"), "{\"numStartups\":3,\"mcpServers\":{\"other\":{\"command\":\"other\"}}}");

    void Because() => _plan = Install(DirectMcpScope.User, _every);

    [Fact] void should_register_claude_in_its_user_configuration() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!["cratis-direct"]!["args"]!.ToJsonString().ShouldEqual("[\"direct\",\"mcp\"]");
    [Fact] void should_keep_other_claude_servers() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!["other"].ShouldNotBeNull();
    [Fact] void should_keep_other_claude_settings() => ReadJson(HomeFile(".claude.json"))["numStartups"]!.GetValue<int>().ShouldEqual(3);
    [Fact] void should_register_codex_as_a_table() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldContain("[mcp_servers.cratis-direct]");
    [Fact] void should_register_copilot_in_the_vs_code_user_configuration() => ReadJson(HomeFile(".config/Code/User/mcp.json"))["servers"]!["cratis-direct"]!["type"]!.GetValue<string>().ShouldEqual("stdio");
    [Fact] void should_register_cursor_in_its_user_configuration() => ReadJson(HomeFile(".cursor/mcp.json"))["mcpServers"]!["cratis-direct"]!["command"]!.GetValue<string>().ShouldEqual("cratis");
    [Fact] void should_register_opencode_as_a_local_command() => ReadJson(HomeFile(".config/opencode/opencode.json"))["mcp"]!["cratis-direct"]!["command"]!.ToJsonString().ShouldEqual("[\"cratis\",\"direct\",\"mcp\"]");
    [Fact] void should_record_ownership_of_every_registration() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly(_every);
    [Fact] void should_report_an_addition_per_client() => _plan.Changes.Select(change => change.Action).ShouldContainOnly("add", "add", "add", "add", "add");
}
