// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_codex_relocated_inside_the_home_directory : given.a_home_and_a_project
{
    void Establish() => _environment["CODEX_HOME"] = HomeFile("tools/codex");

    void Because() => _plan = Install(DirectMcpScope.User, ["codex"]);

    [Fact] void should_register_in_the_relocated_configuration() => File.ReadAllText(HomeFile("tools/codex/config.toml")).ShouldContain("[mcp_servers.cratis-direct]");
    [Fact] void should_show_the_relocated_path() => _plan.Changes.Single().Path.ShouldEqual("~/tools/codex/config.toml");
    [Fact] void should_not_write_the_default_location() => Directory.Exists(HomeFile(".codex")).ShouldBeFalse();
}
