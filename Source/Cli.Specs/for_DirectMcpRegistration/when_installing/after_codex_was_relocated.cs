// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class after_codex_was_relocated : given.a_home_and_a_project
{
    void Establish()
    {
        Install(DirectMcpScope.User, ["claude", "codex"]);
        _environment["CODEX_HOME"] = HomeFile("tools/codex");
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude", "codex"]);

    [Fact] void should_not_report_a_conflict() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_register_in_the_relocated_configuration() => File.ReadAllText(HomeFile("tools/codex/config.toml")).ShouldContain("[mcp_servers.cratis-direct]");
    [Fact] void should_remove_the_earlier_registration() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldNotContain("cratis-direct");
    [Fact] void should_keep_the_other_clients_registration() => IsRegistered(HomeFile(".claude.json"), "mcpServers").ShouldBeTrue();
    [Fact] void should_own_the_relocated_registration() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Path).ShouldContainOnly(".claude.json", "tools/codex/config.toml");
}
