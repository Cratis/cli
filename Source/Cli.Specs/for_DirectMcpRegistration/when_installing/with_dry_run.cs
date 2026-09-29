// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_dry_run : given.a_home_and_a_project
{
    void Because() => _plan = Install(DirectMcpScope.User, ["claude", "codex"], dryRun: true);

    [Fact] void should_write_nothing() => Directory.GetFileSystemEntries(_home).ShouldBeEmpty();
    [Fact] void should_show_where_claude_would_change() => _plan.Changes[0].Path.ShouldEqual("~/.claude.json");
    [Fact] void should_show_the_claude_member() => _plan.Changes[0].Member.ShouldEqual("mcpServers.cratis-direct");
    [Fact] void should_show_the_exact_claude_value() => _plan.Changes[0].Value.ShouldContain("\"command\": \"cratis\"");
    [Fact] void should_show_the_exact_codex_table() => _plan.Changes[1].Value.ShouldEqual("[mcp_servers.cratis-direct]\ncommand = \"cratis\"\nargs = [\"direct\", \"mcp\", \"--url\", \"https://direct.example\", \"--no-tenant\"]");
}
