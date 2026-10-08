// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_the_xdg_configuration_directory_moved : given.a_home_and_a_project
{
    void Establish()
    {
        Install(DirectMcpScope.User, ["copilot", "opencode"]);
        _environment["XDG_CONFIG_HOME"] = HomeFile("xdg");
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [Fact] void should_remove_the_copilot_registration_where_it_was_written() => IsRegistered(HomeFile(".config/Code/User/mcp.json"), "servers").ShouldBeFalse();
    [Fact] void should_remove_the_opencode_registration_where_it_was_written() => IsRegistered(HomeFile(".config/opencode/opencode.json"), "mcp").ShouldBeFalse();
    [Fact] void should_not_write_the_relocated_directory() => Directory.Exists(HomeFile("xdg")).ShouldBeFalse();
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
