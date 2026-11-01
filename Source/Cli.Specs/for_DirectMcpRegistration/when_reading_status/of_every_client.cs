// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_reading_status;

public class of_every_client : given.a_home_and_a_project
{
    IReadOnlyDictionary<string, DirectMcpClientStatus> _status;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude", "cursor"]);
        File.WriteAllText(HomeFile(".cursor/mcp.json"), "{\"mcpServers\":{\"cratis-direct\":{\"command\":\"changed\"}}}");
        Write(HomeFile(".codex/config.toml"), "[mcp_servers.cratis-direct]\ncommand = \"mine\"\nargs = []\n");
    }

    void Because() => _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, []).ToDictionary(client => client.Client);

    [Fact] void should_report_an_owned_registration() => _status["claude"].State.ShouldEqual("registered");
    [Fact] void should_report_what_it_launches() => _status["claude"].Detail.ShouldEqual("cratis direct mcp --url https://direct.example --no-tenant");
    [Fact] void should_report_a_changed_registration() => _status["cursor"].State.ShouldEqual("modified");
    [Fact] void should_report_an_entry_it_did_not_write() => _status["codex"].State.ShouldEqual("user-owned");
    [Fact] void should_report_a_missing_registration() => _status["opencode"].State.ShouldEqual("absent");
    [Fact] void should_report_an_unsupported_client() => _status["pi"].State.ShouldEqual("unsupported");
    [Fact] void should_show_the_home_relative_path() => _status["copilot"].Path.ShouldEqual("~/.config/Code/User/mcp.json");
}
