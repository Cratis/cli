// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildHost;

public class when_forwarding_agent_progress : given.a_project_repository
{
    readonly List<BuildProgress> _progress = [];
    JsonObject _outcome;
    string? _progressPath;

    async Task Because()
    {
        var order = given.an_order.Parse();
        order.Document["contractVersion"] = "2";
        var options = new BuildHostOptions(
            _project,
            null,
            [],
            false,
            null,
            _ => [],
            session => new reporting_agent(session, path => _progressPath = path),
            BuildRedaction.Patterns,
            _log.Add)
        {
            WorkspaceRoot = _workspaces
        };
        _outcome = await new BuildHost(options).Build(order, BuildGitInstructions.Offline, _progress.Add, CancellationToken.None);
    }

    [Fact] void should_include_host_lifecycle_and_agent_activity() => string.Join(',', _progress.Select(item => item.Step)).ShouldEqual("Preparing the build,Building,Reading files,Verifying the build");
    [Fact] void should_provide_the_contract_two_generator_progress_path() => _progressPath.ShouldEqual(BuildWorkspace.At(_workspaces, given.an_order.Parse()).Progress);
    [Fact] void should_preserve_the_agent_outcome() => _outcome["kind"]!.GetValue<string>().ShouldEqual("nothingToDo");

    sealed class reporting_agent(BuildAgentSession session, Action<string> path) : IBuildAgent
    {
        public string Name => "Reporting agent";

        public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            path(environment[ExecGenerator.ProgressVariable]);
            session.Progress(new("Reading files"));
            await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], """{ "kind": "nothingToDo" }""", cancellationToken);
            return new("Done", true, null);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
