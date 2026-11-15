// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_pi_is_outside_home : given.a_setup
{
    void Establish()
    {
        _settings.Clients = ["pi,codex"];
        _environment["PI_CODING_AGENT_DIR"] = Path.Combine(_home, "..", "external-agent");
    }
    async Task Because() => _result = await Run();
    [Fact] void should_refuse_pi() => _result.Clients[0].State.ShouldEqual("refused");
    [Fact] void should_explain_the_variable() => _result.Clients[0].Reason.ShouldContain("PI_CODING_AGENT_DIR");
    [Fact] void should_suggest_project_scope() => _result.Clients[0].Reason.ShouldContain("--scope project");
    [Fact] void should_continue_with_codex() => _result.Clients[1].State.ShouldEqual("installed");
    [Fact] void should_return_failure() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);
}
