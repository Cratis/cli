// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

/// <summary>Claude Code's configuration was written, then writing Cursor's failed before the installation finished.</summary>
public class after_an_interrupted_installation : given.a_home_and_a_project
{
    Exception _interruption;

    void Establish()
    {
        Directory.CreateDirectory(HomeFile(".cursor/mcp.json"));
        _interruption = Catch.Exception(() => Install(DirectMcpScope.User, ["claude", "cursor"]));
        Directory.Delete(HomeFile(".cursor/mcp.json"));
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude", "cursor"]);

    [Fact] void should_have_been_interrupted() => _interruption.ShouldNotBeNull();
    [Fact] void should_own_what_it_wrote_before_the_interruption() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_register_both_clients() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("claude", "cursor");
    [Fact] void should_write_the_client_that_failed() => ReadJson(HomeFile(".cursor/mcp.json"))["mcpServers"]!["cratis-direct"].ShouldNotBeNull();
}
