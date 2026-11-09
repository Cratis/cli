// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ModelingSession;

public class when_unattended : given.a_modeling_board
{
    void Establish() => _polls.Enqueue(Prompt("Queued"));

    Task Because() => Run(unattended: true);

    [Fact] void should_connect_unattended() => CallsTo("studio_connect_local_agent").Single()["mode"]!.GetValue<string>().ShouldEqual("Unattended");
    [Fact] void should_let_the_agent_change_the_board_at_once() => _agent.Prompts.Single().MayChange.ShouldBeTrue();
    [Fact] void should_tell_the_agent_which_board_it_works_on() => _agent.Prompts.Single().Prompt.ShouldContain(Board);
    [Fact] void should_answer_without_a_proposal() => CallsTo("studio_answer_local_agent_prompt").Single()["isProposal"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_take_the_board_tools_away_again_after_the_prompt() => _session.MayChangeTools.ShouldBeFalse();
}
