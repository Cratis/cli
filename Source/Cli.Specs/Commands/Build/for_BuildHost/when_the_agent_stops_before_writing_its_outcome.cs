// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_the_agent_stops_before_writing_its_outcome : given.an_agent_that_stops_before_its_outcome
{
    JsonObject _outcome;

    void Establish() => _writesOutcomeOnPrompt = 2;

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline);

    [Fact] void should_ask_it_to_go_on() => _prompts.Count.ShouldEqual(2);
    [Fact] void should_tell_it_nobody_will_answer() => _prompts[1].ShouldContain("nobody will answer");
    [Fact] void should_repeat_where_the_outcome_goes() => _prompts[1].ShouldContain("write the outcome as JSON to");
    [Fact] void should_report_the_outcome_it_then_wrote() => _outcome["kind"]!.GetValue<string>().ShouldEqual("built");
}
