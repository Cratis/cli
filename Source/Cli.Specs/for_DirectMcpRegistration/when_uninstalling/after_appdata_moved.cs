// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_appdata_moved : given.a_home_and_a_project
{
    void Establish()
    {
        _platform = "windows";
        _environment["APPDATA"] = HomeFile("AppData/Roaming");
        Install(DirectMcpScope.User, ["copilot"]);
        _environment["APPDATA"] = HomeFile("Roaming");
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User, "copilot");

    [Fact] void should_remove_the_registration_where_it_was_written() => IsRegistered(HomeFile("AppData/Roaming/Code/User/mcp.json"), "servers").ShouldBeFalse();
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
