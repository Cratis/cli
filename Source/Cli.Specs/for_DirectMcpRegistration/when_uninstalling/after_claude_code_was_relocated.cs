// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_claude_code_was_relocated : given.a_home_and_a_project
{
    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        _environment["CLAUDE_CONFIG_DIR"] = HomeFile("claude-config");
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [Fact] void should_remove_the_registration_where_it_was_written() => IsRegistered(HomeFile(".claude.json"), "mcpServers").ShouldBeFalse();
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
