// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_the_registration_was_removed : given.a_home_and_a_project
{
    const string Remaining = "{\"mcpServers\":{\"other\":{\"command\":\"other\"}}}";
    DirectMcpClientStatus _status;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude", "cursor"]);
        File.WriteAllText(HomeFile(".claude.json"), Remaining);
        _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, ["claude"]).Single();
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User, "claude");

    [Fact] void should_report_it_as_absent_in_status() => _status.State.ShouldEqual("absent");
    [Fact] void should_not_report_a_conflict() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_the_file_as_the_user_left_it() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual(Remaining);
    [Fact] void should_forget_it() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("cursor");
}
