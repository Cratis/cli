// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_codex_was_relocated : given.a_home_and_a_project
{
    void Establish()
    {
        Install(DirectMcpScope.User, ["codex"]);
        _environment["CODEX_HOME"] = HomeFile("tools/codex");
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User, "codex");

    [Fact] void should_remove_the_registration_where_it_was_written() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldNotContain("cratis-direct");
    [Fact] void should_not_write_the_relocated_configuration() => Directory.Exists(HomeFile("tools/codex")).ShouldBeFalse();
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
