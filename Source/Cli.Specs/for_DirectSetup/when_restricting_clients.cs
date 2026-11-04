// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_restricting_clients : given.a_setup
{
    void Establish()
    {
        Directory.CreateDirectory(HomeFile(".cursor"));
        _settings.Clients = ["codex,claude", "codex"];
    }
    async Task Because() => _result = await Run();
    [Fact] void should_install_only_the_requested_clients() => _result.Clients.Select(client => client.Client).ShouldEqual(["codex", "claude"]);
    [Fact] void should_not_register_an_unselected_client() => File.Exists(HomeFile(".cursor/mcp.json")).ShouldBeFalse();
}
