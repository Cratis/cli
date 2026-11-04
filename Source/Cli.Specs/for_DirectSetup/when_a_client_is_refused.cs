// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_a_client_is_refused : given.a_setup
{
    void Establish()
    {
        _settings.Clients = ["claude,codex"];
        Write(HomeFile(".claude.json"), "{\"mcpServers\":{\"cratis-direct\":{\"command\":\"mine\"}}}");
    }
    async Task Because() => _result = await Run();
    [Fact] void should_fail() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_refusal() => _result.Clients[0].State.ShouldEqual("refused");
    [Fact] void should_continue_installing() => _result.Clients[1].State.ShouldEqual("installed");
    [Fact] void should_leave_the_foreign_entry_alone() => ReadJson(HomeFile(".claude.json"))["mcpServers"]["cratis-direct"]["command"].GetValue<string>().ShouldEqual("mine");
}
