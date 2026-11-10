// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.given;

/// <summary>
/// An agent that ends its turn without writing its outcome until it has been prompted a number of times, as Copilot CLI does
/// when one of its actions is refused.
/// </summary>
public class an_agent_that_stops_before_its_outcome : a_project_repository
{
    protected List<string> _prompts;
    protected int _writesOutcomeOnPrompt;

    void Establish()
    {
        _prompts = [];
        _agent = session => new StoppingAgent(session, this);
    }

    sealed class StoppingAgent(BuildAgentSession session, an_agent_that_stops_before_its_outcome context) : IBuildAgent
    {
        public string Name => "Stopping agent";

        public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            // The kit learns the project's conventions first, through the same agent; only the build's prompts count here.
            if (prompt.Contains("Record the conventions at", StringComparison.Ordinal)) return new BuildAgentReply("learned", true, null);
            context._prompts.Add(prompt);
            if (context._prompts.Count == context._writesOutcomeOnPrompt)
            {
                Directory.CreateDirectory(Path.Combine(session.Project, "src"));
                await File.WriteAllTextAsync(Path.Combine(session.Project, "src", "PayInvoice.txt"), "built PayInvoice\n", cancellationToken);
                await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], context._agentOutcome, cancellationToken);
            }

            return new BuildAgentReply("done", true, null);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
