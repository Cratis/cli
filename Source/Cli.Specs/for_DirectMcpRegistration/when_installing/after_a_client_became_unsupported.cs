// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class after_a_client_became_unsupported : given.a_home_and_a_project
{
    string _claude;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        _claude = File.ReadAllText(HomeFile(".claude.json"));
        _environment["CLAUDE_CONFIG_DIR"] = HomeFile("claude-config");
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude", "cursor"]);

    [Fact] void should_register_the_supported_client() => ReadJson(HomeFile(".cursor/mcp.json"))["mcpServers"]!["cratis-direct"].ShouldNotBeNull();
    [Fact] void should_leave_the_earlier_registration_alone() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual(_claude);
    [Fact] void should_keep_owning_the_earlier_registration() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("claude", "cursor");
}
