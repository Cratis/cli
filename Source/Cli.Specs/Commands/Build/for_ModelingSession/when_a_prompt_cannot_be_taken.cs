// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ModelingSession;

public class when_a_prompt_cannot_be_taken : given.a_modeling_board
{
    const string Other = "2b3c4d5e-6f70-4812-9a3b-4c5d6e7f8091";

    void Establish()
    {
        _polls.Enqueue(new System.Text.Json.Nodes.JsonArray(Prompt("Queued")[0]!.DeepClone(), Prompt("Queued", "Rename the slice", Other)[0]!.DeepClone()));
        _refusedTakes.Add(PromptId);
    }

    Task Because() => Run(unattended: true);

    [Fact] void should_not_hand_it_to_the_agent() => _agent.Prompts.Single().Prompt.ShouldContain("Rename the slice");
    [Fact] void should_answer_only_the_prompt_it_took() => CallsTo("studio_answer_local_agent_prompt").Single()["promptId"]!.GetValue<string>().ShouldEqual(Other);
}
