// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_reading_status;

public class after_claude_code_was_relocated : given.a_home_and_a_project
{
    DirectMcpClientStatus _status;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        _environment["CLAUDE_CONFIG_DIR"] = HomeFile("claude-config");
    }

    void Because() => _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, ["claude"]).Single();

    [Fact] void should_report_the_registration() => _status.State.ShouldEqual("registered");
    [Fact] void should_report_where_it_was_written() => _status.Path.ShouldEqual("~/.claude.json");
    [Fact] void should_say_uninstall_still_removes_it() => _status.Detail.ShouldContain("'uninstall' still removes this registration");
}
