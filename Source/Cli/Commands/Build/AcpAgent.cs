// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A coding agent spoken to over the Agent Client Protocol. The host is the client: it starts the agent, opens one session
/// with the session's tools, answers the agent's permission requests and file reads and writes from the session's
/// permissions, and collects what the agent says.
/// </summary>
/// <param name="session">The session.</param>
/// <param name="name">The agent's name, for people.</param>
/// <param name="program">The command that starts the agent.</param>
/// <param name="arguments">Its arguments.</param>
public sealed class AcpAgent(BuildAgentSession session, string name, string program, IReadOnlyList<string> arguments) : IBuildAgent
{
    readonly StringBuilder _reply = new();
    readonly List<McpHttpBridge> _bridges = [];
    Process? _process;
    AcpConnection? _connection;
    Task? _listening;
    CancellationTokenSource? _stop;
    string? _sessionId;
    bool _guided;

    /// <inheritdoc/>
    public string Name => name;

    /// <inheritdoc/>
    public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        try
        {
            await Start(environment, cancellationToken);
            lock (_reply)
            {
                _reply.Clear();
            }

            var blocks = new JsonArray();
            if (!_guided)
            {
                foreach (var block in Guidance()) blocks.Add(block);
                _guided = true;
            }

            blocks.Add(new JsonObject { ["type"] = "text", ["text"] = prompt });
            session.Log($"{Name} is working.");
            var result = await _connection!.Request("session/prompt", new JsonObject { ["sessionId"] = _sessionId, ["prompt"] = blocks }, cancellationToken);
            var stopReason = result?["stopReason"]?.GetValue<string>() ?? "end_turn";
            string reply;
            lock (_reply)
            {
                reply = _reply.ToString().Trim();
            }
            var finished = string.Equals(stopReason, "end_turn", StringComparison.Ordinal) || string.Equals(stopReason, "max_turn_requests", StringComparison.Ordinal);
            return finished
                ? new(reply, true, null)
                : new(reply, false, $"{Name} stopped: {stopReason}.");
        }
        catch (AcpError error)
        {
            return BuildAgentReply.Failure($"{Name} answered with an error: {error.Message}");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_stop is not null) await _stop.CancelAsync();
        if (_process is { HasExited: false })
        {
            try
            {
                _process.StandardInput.Close();
                using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _process.WaitForExitAsync(grace.Token);
            }
            catch (OperationCanceledException)
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (_listening is not null) await Task.WhenAny(_listening, Task.Delay(2000));
        foreach (var bridge in _bridges) await bridge.DisposeAsync();
        _connection?.Dispose();
        _process?.Dispose();
        _stop?.Dispose();
    }

    async Task Start(IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        if (_connection is not null) return;
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = session.Project,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        foreach (var (key, setting) in environment) start.Environment[key] = setting;

        _process = Process.Start(start) ?? throw new AcpError(AcpError.Refused, $"{program} could not be started.");
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginErrorReadLine();
        _stop = new CancellationTokenSource();
        _connection = new AcpConnection(_process.StandardOutput, _process.StandardInput, Handle, OnNotification);
        _listening = _connection.Listen(_stop.Token);

        var initialize = new JsonObject
        {
            ["protocolVersion"] = 1,
            ["clientCapabilities"] = new JsonObject { ["fs"] = new JsonObject { ["readTextFile"] = true, ["writeTextFile"] = true }, ["terminal"] = false },
            ["clientInfo"] = new JsonObject { ["name"] = "cratis-build", ["version"] = typeof(AcpAgent).Assembly.GetName().Version?.ToString() ?? "0" }
        };
        var initialized = await _connection.Request("initialize", initialize, cancellationToken);
        var capabilities = initialized?["agentCapabilities"];
        var http = capabilities?["mcpCapabilities"]?["http"] is JsonValue value && value.TryGetValue<bool>(out var supported) && supported;

        var opening = new JsonObject { ["cwd"] = session.Project, ["mcpServers"] = AcpSessionTools.For(Reachable(session.Tools, http), http) };
        var created = await _connection.Request("session/new", opening, cancellationToken);
        _sessionId = created?["sessionId"]?.GetValue<string>() ?? throw new AcpError(AcpError.Refused, $"{Name} opened no session.");
    }

    /// <summary>
    /// Gives an agent that connects over HTTP the local servers over HTTP too: some (Copilot CLI) refuse every stdio server a
    /// client hands them, and the agent would build without the Screenplay tools.
    /// </summary>
    /// <param name="tools">The session's servers.</param>
    /// <param name="http">Whether the agent connects to servers over HTTP.</param>
    /// <returns>The servers as the agent reaches them.</returns>
    List<BuildAgentTool> Reachable(IEnumerable<BuildAgentTool> tools, bool http)
    {
        var reachable = new List<BuildAgentTool>();
        foreach (var tool in tools)
        {
            if (!http || tool.Command is null)
            {
                reachable.Add(tool);
                continue;
            }

            var bridge = McpHttpBridge.Start(tool.Command, tool.Arguments, session.Project);
            _bridges.Add(bridge);
            reachable.Add(bridge.Describe(tool));
        }

        return reachable;
    }

    IEnumerable<JsonObject> Guidance()
    {
        // The guidance's instruction file links its skills by relative path; the agent reads them through the client,
        // which permits reads under the guidance folder.
        if (session.Guidance?.Instructions is { } instructions)
        {
            yield return new JsonObject
            {
                ["type"] = "resource_link",
                ["uri"] = new Uri(instructions).AbsoluteUri,
                ["name"] = "Screenplay session guidance"
            };
        }
        else if (session.Guidance?.Context is { } context)
        {
            yield return new JsonObject { ["type"] = "text", ["text"] = File.ReadAllText(context) };
        }

        if (session.Instructions.Length > 0) yield return new JsonObject { ["type"] = "text", ["text"] = session.Instructions };
    }

    async Task<JsonNode?> Handle(string method, JsonNode? parameters, CancellationToken cancellationToken)
    {
        switch (method)
        {
            case "session/request_permission":
                return AcpPermissionDecision.Answer(session, parameters);

            case "fs/read_text_file":
            {
                var path = parameters?["path"]?.GetValue<string>() ?? string.Empty;
                if (!session.Permissions.AllowsRead(path) || !File.Exists(path)) throw new AcpError(AcpError.Refused, $"Reading {path} is not permitted.");
                var lines = await File.ReadAllLinesAsync(path, cancellationToken);
                var first = Math.Max(1, parameters?["line"]?.GetValue<int>() ?? 1);
                var limit = parameters?["limit"]?.GetValue<int>() ?? int.MaxValue;
                return new JsonObject { ["content"] = string.Join('\n', lines.Skip(first - 1).Take(limit)) };
            }

            case "fs/write_text_file":
            {
                var path = parameters?["path"]?.GetValue<string>() ?? string.Empty;
                if (!session.Permissions.AllowsWrite(path))
                {
                    session.Log($"Refused the agent's write to {path}.");
                    throw new AcpError(AcpError.Refused, $"Writing {path} is not permitted by the build kit.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                await File.WriteAllTextAsync(path, parameters?["content"]?.GetValue<string>() ?? string.Empty, cancellationToken);
                return null;
            }

            default:
                throw new AcpError(AcpError.MethodNotFound, $"The client does not offer {method}.");
        }
    }

    void OnNotification(string method, JsonNode? parameters)
    {
        if (method != "session/update") return;
        var update = parameters?["update"];
        var kind = update?["sessionUpdate"]?.GetValue<string>();
        if (kind == "agent_message_chunk" && update?["content"]?["text"]?.GetValue<string>() is string text)
        {
            lock (_reply)
            {
                _reply.Append(text);
            }
        }
        else if (kind == "tool_call" && update?["title"]?.GetValue<string>() is string title)
        {
            session.Log($"{Name}: {title}");
        }
    }
}
