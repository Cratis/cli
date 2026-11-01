// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_reading_status;

public class after_appdata_moved : given.a_home_and_a_project
{
    DirectMcpClientStatus _status;

    void Establish()
    {
        _platform = "windows";
        _environment["APPDATA"] = HomeFile("AppData/Roaming");
        Install(DirectMcpScope.User, ["copilot"]);
        _environment["APPDATA"] = Path.Combine(_project, "Roaming");
    }

    void Because() => _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, ["copilot"]).Single();

    [Fact] void should_report_the_registration() => _status.State.ShouldEqual("registered");
    [Fact] void should_report_where_it_was_written() => _status.Path.ShouldEqual("~/AppData/Roaming/Code/User/mcp.json");
    [Fact] void should_say_install_can_no_longer_register_it() => _status.Detail.ShouldContain("'install' can no longer register this client");
}
