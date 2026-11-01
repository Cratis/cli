// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_reading_status;

public class after_the_xdg_configuration_directory_moved : given.a_home_and_a_project
{
    IReadOnlyDictionary<string, DirectMcpClientStatus> _status;

    void Establish()
    {
        Install(DirectMcpScope.User, ["copilot", "opencode"]);
        File.WriteAllText(HomeFile(".config/Code/User/mcp.json"), "{\"servers\":{}}");
        File.WriteAllText(HomeFile(".config/opencode/opencode.json"), "{\"mcp\":{\"cratis-direct\":{\"type\":\"local\",\"command\":[\"changed\"]}}}");
        _environment["XDG_CONFIG_HOME"] = HomeFile("xdg");
    }

    void Because() => _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, ["copilot", "opencode"]).ToDictionary(client => client.Client);

    [Fact] void should_report_a_removed_registration_as_absent() => _status["copilot"].State.ShouldEqual("absent");
    [Fact] void should_report_where_the_removed_registration_was_written() => _status["copilot"].Path.ShouldEqual("~/.config/Code/User/mcp.json");
    [Fact] void should_report_a_changed_registration_as_modified() => _status["opencode"].State.ShouldEqual("modified");
    [Fact] void should_report_where_the_changed_registration_was_written() => _status["opencode"].Path.ShouldEqual("~/.config/opencode/opencode.json");
}
