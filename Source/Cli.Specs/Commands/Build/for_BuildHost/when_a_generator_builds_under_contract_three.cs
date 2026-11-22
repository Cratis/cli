// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_a_generator_builds_under_contract_three : given.a_project_repository
{
    string? _contractThree;
    string? _contractOne;

    async Task Because()
    {
        _agent = session => new capturing_agent(path => _contractThree = path);
        var three = given.an_order.Parse();
        three.Document["contractVersion"] = "3";
        await Build(BuildGitInstructions.Offline, three);
        _agent = session => new capturing_agent(path => _contractOne = path);
        await Build(BuildGitInstructions.Offline, given.an_order.Parse());
    }

    [Fact] void should_provide_the_progress_file() => _contractThree.ShouldEqual(BuildWorkspace.At(_workspaces, given.an_order.Parse()).Progress);
    [Fact] void should_keep_progress_away_from_contract_one() => _contractOne.ShouldBeNull();

    sealed class capturing_agent(Action<string?> progress) : IBuildAgent
    {
        public string Name => "Capturing agent";

        public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            if (!environment.TryGetValue(ExecGenerator.OutcomeVariable, out var outcome)) return new("learned", true, null);
            progress(environment.GetValueOrDefault(ExecGenerator.ProgressVariable));
            await File.WriteAllTextAsync(outcome, """{ "kind": "nothingToDo" }""", cancellationToken);
            return new("Done", true, null);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
