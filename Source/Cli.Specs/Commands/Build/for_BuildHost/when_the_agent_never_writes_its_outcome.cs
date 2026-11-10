// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_the_agent_never_writes_its_outcome : given.an_agent_that_stops_before_its_outcome
{
    JsonObject _outcome;

    void Establish() => _writesOutcomeOnPrompt = int.MaxValue;

    async Task Because() => _outcome = await Build(BuildGitInstructions.Offline);

    [Fact] void should_stop_asking_after_two_more_prompts() => _prompts.Count.ShouldEqual(3);
    [Fact] void should_not_report_it_built() => _outcome["kind"]!.GetValue<string>().ShouldNotEqual("built");
}
