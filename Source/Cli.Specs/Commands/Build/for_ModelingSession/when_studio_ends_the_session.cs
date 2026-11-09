// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ModelingSession;

public class when_studio_ends_the_session : given.a_modeling_board
{
    void Establish()
    {
        _polls.Enqueue(Prompt("Queued"));
        _studioEndsTheSession = true;
    }

    Task Because() => Run(unattended: true);

    [Fact] void should_stop_without_handing_on_prompts() => _agent.Prompts.ShouldBeEmpty();
    [Fact] void should_stop_without_answering() => _answered.ShouldEqual(0);
    [Fact] void should_not_disconnect_a_session_studio_already_ended() => CallsTo("studio_disconnect_local_agent").ShouldBeEmpty();
    [Fact] void should_stop_the_agent() => _agent.Disposed.ShouldBeTrue();
}
