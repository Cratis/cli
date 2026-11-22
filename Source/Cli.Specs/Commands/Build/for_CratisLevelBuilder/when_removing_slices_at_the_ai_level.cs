// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_CratisLevelBuilder;

public class when_removing_slices_at_the_ai_level : Specification
{
    string _root;
    string _project;
    RecordingAgent _agent;

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
        await File.WriteAllTextAsync(orderFile, given.an_order.RemovalJson());
        var session = given.a_session.With() with { Project = _project };
        var environment = new Dictionary<string, string> { [ExecGenerator.OrderVariable] = orderFile, [ExecGenerator.OutcomeVariable] = Path.Combine(_root, "outcome.json") };
        await using var builder = new CratisLevelBuilder(session, BuildLevel.AI, "cratis", _agent);
        await builder.Prompt("Remove CancelInvoice.", environment, CancellationToken.None);
    }

    [Fact] void should_forward_the_prompt_unchanged() => _agent.Prompts.ShouldContainOnly("Remove CancelInvoice.");
    [Fact] void should_render_nothing() => Directory.GetFileSystemEntries(_project).ShouldBeEmpty();

    sealed class RecordingAgent : IBuildAgent
    {
        public List<string> Prompts { get; } = [];

        public string Name => "Recording agent";

        public Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            Prompts.Add(prompt);
            return Task.FromResult(new BuildAgentReply("done", true, null));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
