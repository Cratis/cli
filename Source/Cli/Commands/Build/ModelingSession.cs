// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Studio;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Connects a local coding agent to a Studio board for modeling, through Studio's local agent tools. People hand it work
/// by mentioning it in the board chat; the session takes each queued prompt, hands it to the agent and answers in the
/// chat. In propose-and-confirm mode the agent may only read the board until a person confirms its proposal; unattended,
/// it acts at once.
/// </summary>
/// <param name="studio">Calls a Studio MCP tool by name.</param>
/// <param name="start">Starts the agent for the session Studio connected, so its own calls to Studio carry the session.</param>
/// <param name="options">Which board, under what name, in which mode.</param>
/// <param name="log">Where the session writes what it is doing.</param>
/// <param name="delay">Waits between polls; replaceable in specifications.</param>
public sealed class ModelingSession(
    Func<string, JsonObject, CancellationToken, Task<JsonNode?>> studio,
    Func<string, ModelingAgent> start,
    ModelingSessionOptions options,
    Action<string> log,
    Func<TimeSpan, CancellationToken, Task>? delay = null)
{
    /// <summary>What the agent shows beside it while it works on a prompt.</summary>
    public const string Working = "Working on a prompt";

    /// <summary>The header that marks the agent's own calls to Studio as the session's, so the board attributes them to it.</summary>
    public const string SessionHeader = "x-cratis-studio-local-agent";

    /// <summary>What the agent shows beside it while it waits: nothing.</summary>
    public const string Idle = "";

    /// <summary>How often the session asks for new prompts while the agent waits.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <summary>How often the session tells Studio it is still there while the agent works; well inside Studio's 30 seconds.</summary>
    public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(20);

    readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    string _sessionId = string.Empty;
    bool _ended;

    /// <summary>
    /// Connects, delivers prompts until cancelled or until Studio ends the session, and disconnects.
    /// </summary>
    /// <param name="cancellationToken">Stops the session.</param>
    /// <returns>The number of prompts answered.</returns>
    /// <exception cref="ModelingSessionRefused">Studio refused to connect the agent.</exception>
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var connected = await studio(
            "studio_connect_local_agent",
            new JsonObject { ["eventModelId"] = options.EventModelId, ["name"] = options.Name, ["mode"] = options.Mode },
            cancellationToken);
        if (!IsTrue(connected?["connected"]) || connected?["sessionId"]?.ToString() is not { Length: > 0 } sessionId)
        {
            throw new ModelingSessionRefused(connected?["outcome"]?.ToString() is { Length: > 0 } outcome ? outcome : "Studio did not connect the agent.");
        }

        _sessionId = sessionId;
        log($"{options.Name} is connected to the board{(options.Unattended ? ", acting without asking" : ", proposing changes for a person to confirm")}. Mention it in the board chat.");

        var answered = 0;
        var agent = start(sessionId);
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_ended)
            {
                foreach (var prompt in await Poll(Idle, cancellationToken))
                {
                    if (_ended || cancellationToken.IsCancellationRequested) break;
                    if (await Deliver(agent, prompt, cancellationToken)) answered++;
                }

                if (!_ended) await _delay(PollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await agent.Agent.DisposeAsync();
            if (!_ended) await Disconnect();
        }

        return answered;
    }

    static string Safe(BuildAgentSession session, string text) =>
        BuildRedaction.Patterns.Redact(text
            .Replace(session.Workspace, "the session's workspace", StringComparison.Ordinal)
            .Replace(session.Project, "the project", StringComparison.Ordinal));

    static bool IsTrue(JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out var flag) && flag;

    async Task<bool> Deliver(ModelingAgent agent, ModelingPrompt prompt, CancellationToken cancellationToken)
    {
        try
        {
            await studio("studio_take_local_agent_prompt", Arguments(prompt.Id), cancellationToken);
        }
        catch (StudioMcpError refused)
        {
            log($"Skipped a prompt Studio would not hand over: {refused.Message}");
            return false;
        }

        var (text, mayChange, isProposal) = ModelingPrompts.For(prompt, options.EventModelId, options.Unattended);
        agent.Session.MayChangeTools = mayChange;
        log($"Working on: {prompt.Text}");

        var reply = await PromptKeepingAlive(agent, text, cancellationToken);

        if (_ended) return false;
        var answer = reply.Succeeded && reply.Reply.Length > 0 ? reply.Reply : $"I could not finish this: {Safe(agent.Session, reply.Error ?? "the agent gave no answer.")}";
        var answering = Arguments(prompt.Id);
        answering["reply"] = answer;
        answering["isProposal"] = isProposal && reply.Succeeded;
        await studio("studio_answer_local_agent_prompt", answering, cancellationToken);
        return true;
    }

    async Task<BuildAgentReply> PromptKeepingAlive(ModelingAgent agent, string text, CancellationToken cancellationToken)
    {
        using var working = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = working.Token;
        var keepingAlive = KeepAlive(working.Cancel, token);
        try
        {
            return await agent.Agent.Prompt(text, new Dictionary<string, string>(), token);
        }
        catch (OperationCanceledException) when (_ended && !cancellationToken.IsCancellationRequested)
        {
            return BuildAgentReply.Failure("the session ended while the agent worked.");
        }
        finally
        {
            agent.Session.MayChangeTools = false;
            await working.CancelAsync();
            await keepingAlive;
        }
    }

    async Task KeepAlive(Action stopWorking, CancellationToken working)
    {
        try
        {
            await Poll(Working, working);
            while (!working.IsCancellationRequested && !_ended)
            {
                await _delay(KeepAliveInterval, working);
                await Poll(Working, working);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (StudioMcpError error)
        {
            log($"Could not tell Studio the agent is still working: {error.Message}");
        }

        if (_ended && !working.IsCancellationRequested) stopWorking();
    }

    async Task<IReadOnlyList<ModelingPrompt>> Poll(string activity, CancellationToken cancellationToken)
    {
        var polling = new JsonObject { ["sessionId"] = _sessionId, ["eventModelId"] = options.EventModelId, ["activity"] = activity };
        var answer = await studio("studio_poll_local_agent", polling, cancellationToken);
        if (!IsTrue(answer?["connected"]))
        {
            if (!_ended) log("Studio ended the session; the agent has left the board.");
            _ended = true;
            return [];
        }

        return ModelingPrompt.ListFrom(answer?["prompts"]);
    }

    async Task Disconnect()
    {
        try
        {
            await studio("studio_disconnect_local_agent", new JsonObject { ["sessionId"] = _sessionId, ["eventModelId"] = options.EventModelId }, CancellationToken.None);
        }
        catch (Exception error) when (error is StudioMcpError or HttpRequestException)
        {
            log($"Could not tell Studio the agent left: {error.Message}");
        }
    }

    JsonObject Arguments(string promptId) => new() { ["sessionId"] = _sessionId, ["eventModelId"] = options.EventModelId, ["promptId"] = promptId };
}
