// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Studio;

namespace Cratis.Cli.Commands.Build.given;

public class a_modeling_board : Specification
{
    public const string Board = "0b9d1f0e-6a4c-4c1e-8f5a-3e2d1c0b9a87";
    public const string SessionId = "1d2c3b4a-5e6f-4a7b-8c9d-0e1f2a3b4c5d";
    public const string PromptId = "7c3e1a2b-0d4f-4e5a-9b6c-1d2e3f4a5b6c";

    protected List<(string Tool, JsonObject Arguments)> _calls;
    protected Queue<JsonArray> _polls;
    protected JsonObject _connected;
    protected HashSet<string> _refusedTakes;
    protected bool _studioEndsTheSession;
    protected FakeAgent _agent;
    protected BuildAgentSession _session;
    protected string? _startedFor;
    protected CancellationTokenSource _stop;
    protected int _answered;

    protected IEnumerable<JsonObject> CallsTo(string tool) => _calls.Where(call => call.Tool == tool).Select(call => call.Arguments);

    void Establish()
    {
        _calls = [];
        _polls = new();
        _refusedTakes = [];
        _connected = new JsonObject { ["connected"] = true, ["outcome"] = "Connected.", ["sessionId"] = SessionId };
        _stop = new();
        _session = a_session.With(mayChangeTools: false);
        _agent = new(_session);
    }

    void Destroy() => _stop.Dispose();

    protected static JsonArray Prompt(string state, string text = "Add an event for a cancelled invoice", string id = PromptId) =>
        [new JsonObject { ["id"] = id, ["eventModelId"] = Board, ["sessionId"] = SessionId, ["prompt"] = text, ["state"] = state }];

    protected async Task Run(bool unattended)
    {
        var session = new ModelingSession(
            Studio,
            sessionId =>
            {
                _startedFor = sessionId;
                return new(_agent, _session);
            },
            new(Board, "Claude Code", unattended),
            _ => { },
            (interval, cancellationToken) => interval == ModelingSession.KeepAliveInterval ? Task.Delay(Timeout.Infinite, cancellationToken) : Task.CompletedTask);
        _answered = await session.Run(_stop.Token);
    }

    async Task<JsonNode?> Studio(string tool, JsonObject arguments, CancellationToken cancellationToken)
    {
        _calls.Add((tool, (JsonObject)arguments.DeepClone()));
        switch (tool)
        {
            case "studio_connect_local_agent":
                return _connected;
            case "studio_poll_local_agent" when _studioEndsTheSession:
                return new JsonObject { ["connected"] = false, ["prompts"] = new JsonArray() };
            case "studio_poll_local_agent" when arguments["activity"]!.GetValue<string>() == ModelingSession.Working:
                return new JsonObject { ["connected"] = true, ["prompts"] = new JsonArray() };
            case "studio_poll_local_agent":
                if (_polls.Count == 0)
                {
                    await _stop.CancelAsync();
                    return new JsonObject { ["connected"] = true, ["prompts"] = new JsonArray() };
                }

                return new JsonObject { ["connected"] = true, ["prompts"] = _polls.Dequeue() };
            case "studio_take_local_agent_prompt" when _refusedTakes.Contains(arguments["promptId"]!.GetValue<string>()):
                throw new StudioMcpError("The prompt was not found.");
            case "studio_answer_local_agent_prompt":
                return new JsonObject { ["answered"] = true, ["replyId"] = Guid.NewGuid().ToString() };
            default:
                return new JsonObject { ["taken"] = true };
        }
    }

    public sealed class FakeAgent(BuildAgentSession session) : IBuildAgent
    {
        public List<(string Prompt, bool MayChange)> Prompts { get; } = [];

        public BuildAgentReply Reply { get; set; } = new("I would add the event Invoice Cancelled.", true, null);

        public bool Disposed { get; private set; }

        public string Name => "Claude Code";

        public Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            Prompts.Add((prompt, session.MayChangeTools));
            return Task.FromResult(Reply);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
