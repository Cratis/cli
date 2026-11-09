// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ModelingSession;

public class when_the_agent_fails : given.a_modeling_board
{
    const string Token = "ghp_abcdefghijklmnopqrstuvwxyz0123456789";
    string _reply;

    void Establish()
    {
        _polls.Enqueue(Prompt("Queued"));
        _agent.Reply = BuildAgentReply.Failure($"could not write {given.a_session.Project}/notes.md with {Token}");
    }

    async Task Because()
    {
        await Run(unattended: true);
        _reply = CallsTo("studio_answer_local_agent_prompt").Single()["reply"]!.GetValue<string>();
    }

    [Fact] void should_still_answer_the_prompt() => _reply.ShouldContain("I could not finish this");
    [Fact] void should_not_reveal_local_paths() => _reply.ShouldNotContain(given.a_session.Project);
    [Fact] void should_not_reveal_credentials() => _reply.ShouldNotContain(Token);
    [Fact] void should_not_answer_with_a_proposal() => CallsTo("studio_answer_local_agent_prompt").Single()["isProposal"]!.GetValue<bool>().ShouldBeFalse();
}
