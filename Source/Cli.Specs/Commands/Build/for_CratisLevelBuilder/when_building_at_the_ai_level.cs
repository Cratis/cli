// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_CratisLevelBuilder;

public class when_building_at_the_ai_level : Specification
{
    string _root;
    string _project;
    BuildAgentReply _reply;
    RecordingAgent _agent;
    string _outcome;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"cratis-level-{Guid.NewGuid():N}");
        _agent = new RecordingAgent();
    }

    void Destroy() => Directory.Delete(_root, true);

    async Task Because()
    {
        _project = Path.Combine(_root, "project");
        Directory.CreateDirectory(_project);
        var orderFile = Path.Combine(_root, "order.json");
        await File.WriteAllTextAsync(orderFile, given.an_order.WithInstructions());
        var session = given.a_session.With() with { Project = _project };
        _outcome = Path.Combine(_root, "outcome.json");
        var environment = new Dictionary<string, string> { [ExecGenerator.OrderVariable] = orderFile, [ExecGenerator.OutcomeVariable] = _outcome };
        await using var builder = new CratisLevelBuilder(session, BuildLevel.AI, "axon", _agent);
        _reply = await builder.Prompt("Build PayInvoice.", environment, CancellationToken.None);
        await builder.Prompt("Go on.", environment, CancellationToken.None);
    }

    [Fact] void should_succeed() => _reply.Succeeded.ShouldBeTrue();
    [Fact] void should_write_the_model_into_the_project() => File.ReadAllText(Path.Combine(_project, "screenplay", "Billing.play")).ShouldEqual("module Billing");
    [Fact] void should_not_render() => Directory.GetFileSystemEntries(_project).ShouldContainOnly(Path.Combine(_project, "screenplay"));
    [Fact] void should_hand_the_agent_the_ai_only_prompt() => _agent.Prompts[0].ShouldEqual(CratisLevelBuilder.AiOnlyPrompt("Build PayInvoice.", "axon"));
    [Fact] void should_say_no_renderer_has_run() => _agent.Prompts[0].ShouldContain("no renderer has run");
    [Fact] void should_name_what_the_agent_writes() => _agent.Prompts[0].ShouldContain("a Java application on Axon Framework");
    [Fact] void should_tell_the_agent_what_to_implement() => _agent.Prompts[0].ShouldContain(CratisLevelBuilder.ModelBehavior);
    [Fact] void should_pass_later_prompts_to_the_agent_unchanged() => _agent.Prompts[1].ShouldEqual("Go on.");
    [Fact] void should_report_the_model_with_the_agents_files() => Files().ShouldContainOnly("src/PayInvoice.java", "screenplay/Billing.play");

    IEnumerable<string> Files() => JsonNode.Parse(File.ReadAllText(_outcome))!["files"]!.AsArray().Select(file => file!.GetValue<string>());

    sealed class RecordingAgent : IBuildAgent
    {
        public List<string> Prompts { get; } = [];

        public string Name => "Recording agent";

        public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            Prompts.Add(prompt);
            await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], "{\"kind\":\"built\",\"files\":[\"src/PayInvoice.java\"]}", cancellationToken);
            return new BuildAgentReply("done", true, null);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
