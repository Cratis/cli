// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class for_copilot_on_windows : given.a_home_and_a_project
{
    void Establish()
    {
        _platform = "windows";
        _environment["APPDATA"] = HomeFile("AppData/Roaming");
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["copilot"]);

    [Fact] void should_register_in_the_vs_code_user_configuration() => ReadJson(HomeFile("AppData/Roaming/Code/User/mcp.json"))["servers"]!["cratis-direct"].ShouldNotBeNull();
}
