// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_ModelingSession;

public class when_proposing_and_confirming : given.a_modeling_board
{
    JsonObject[] _answers;

    void Establish()
    {
        _polls.Enqueue(Prompt("Queued"));
        _polls.Enqueue(Prompt("Confirmed"));
    }

    async Task Because()
    {
        await Run(unattended: false);
        _answers = [.. CallsTo("studio_answer_local_agent_prompt")];
    }

    [Fact] void should_connect_to_the_board_asking_to_propose_first() => CallsTo("studio_connect_local_agent").Single()["mode"]!.GetValue<string>().ShouldEqual("ProposeAndConfirm");
    [Fact] void should_connect_under_the_agents_name() => CallsTo("studio_connect_local_agent").Single()["name"]!.GetValue<string>().ShouldEqual("Claude Code");
    [Fact] void should_start_the_agent_for_the_session_studio_connected() => _startedFor.ShouldEqual(SessionId);
    [Fact] void should_take_each_prompt_before_the_agent_sees_it() => CallsTo("studio_take_local_agent_prompt").Count().ShouldEqual(2);
    [Fact] void should_let_the_agent_only_read_while_it_proposes() => _agent.Prompts[0].MayChange.ShouldBeFalse();
    [Fact] void should_let_the_agent_change_the_board_once_confirmed() => _agent.Prompts[1].MayChange.ShouldBeTrue();
    [Fact] void should_answer_the_queued_prompt_with_a_proposal() => _answers[0]["isProposal"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_answer_the_confirmed_prompt_without_a_proposal() => _answers[1]["isProposal"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_answer_with_what_the_agent_said() => _answers[0]["reply"]!.GetValue<string>().ShouldEqual("I would add the event Invoice Cancelled.");
    [Fact] void should_answer_in_the_session() => _answers.All(answer => answer["sessionId"]!.GetValue<string>() == SessionId).ShouldBeTrue();
    [Fact] void should_show_the_agent_working_while_it_works() => CallsTo("studio_poll_local_agent").Any(poll => poll["activity"]!.GetValue<string>() == ModelingSession.Working).ShouldBeTrue();
    [Fact] void should_count_both_answers() => _answered.ShouldEqual(2);
    [Fact] void should_leave_the_board_when_stopped() => CallsTo("studio_disconnect_local_agent").Single()["sessionId"]!.GetValue<string>().ShouldEqual(SessionId);
    [Fact] void should_stop_the_agent() => _agent.Disposed.ShouldBeTrue();
}
