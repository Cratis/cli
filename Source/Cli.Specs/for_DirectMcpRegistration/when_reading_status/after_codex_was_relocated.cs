// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_reading_status;

public class after_codex_was_relocated : given.a_home_and_a_project
{
    IReadOnlyDictionary<string, DirectMcpClientStatus> _status;

    void Establish()
    {
        _environment["CODEX_HOME"] = HomeFile("tools/codex");
        Install(DirectMcpScope.User, ["claude", "codex"]);
        _environment.Remove("CODEX_HOME");
    }

    void Because() => _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, []).ToDictionary(client => client.Client);

    [Fact] void should_report_the_registration() => _status["codex"].State.ShouldEqual("registered");
    [Fact] void should_report_where_it_was_written() => _status["codex"].Path.ShouldEqual("~/tools/codex/config.toml");
    [Fact] void should_say_where_the_client_reads_now() => _status["codex"].Detail.ShouldContain("The client now reads ~/.codex/config.toml");
    [Fact] void should_report_the_other_clients_registration() => _status["claude"].State.ShouldEqual("registered");
    [Fact] void should_report_a_client_without_a_registration() => _status["cursor"].State.ShouldEqual("absent");
}
