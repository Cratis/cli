// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

/// <summary>Codex was registered in a relocated directory inside the home directory, then relocated where it cannot be registered.</summary>
public class after_codex_was_relocated_outside_the_home_directory : given.a_home_and_a_project
{
    void Establish()
    {
        _environment["CODEX_HOME"] = HomeFile("tools/codex");
        Install(DirectMcpScope.User, ["codex", "cursor"]);
        _environment["CODEX_HOME"] = Path.Combine(_project, "codex");
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [Fact] void should_remove_the_registration_where_it_was_written() => File.ReadAllText(HomeFile("tools/codex/config.toml")).ShouldNotContain("cratis-direct");
    [Fact] void should_remove_the_other_clients_registration() => IsRegistered(HomeFile(".cursor/mcp.json"), "mcpServers").ShouldBeFalse();
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
