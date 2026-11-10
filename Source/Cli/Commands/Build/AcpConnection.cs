// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The client end of an Agent Client Protocol connection: JSON-RPC 2.0, one message per line, over an agent's standard
/// input and output. Requests the agent sends to the client are answered by a handler.
/// </summary>
/// <param name="input">What the agent writes.</param>
/// <param name="output">What the agent reads.</param>
/// <param name="handle">Answers a request from the agent: its result, or throws <see cref="AcpError"/>.</param>
/// <param name="notify">Receives a notification from the agent.</param>
public sealed class AcpConnection(TextReader input, TextWriter output, Func<string, JsonNode?, CancellationToken, Task<JsonNode?>> handle, Action<string, JsonNode?> notify) : IDisposable
{
    readonly ConcurrentDictionary<long, TaskCompletionSource<JsonNode?>> _pending = new();
    readonly SemaphoreSlim _writing = new(1, 1);
    long _nextId;

    /// <summary>
    /// Reads messages until the agent closes its output.
    /// </summary>
    /// <param name="cancellationToken">Stops reading.</param>
    /// <returns>A task that completes when the agent closed its output.</returns>
    public async Task Listen(CancellationToken cancellationToken)
    {
        try
        {
            while (await input.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                JsonObject? message;
                try
                {
                    message = JsonNode.Parse(line) as JsonObject;
                }
                catch (JsonException)
                {
                    continue;
                }

                if (message is not null) Dispatch(message, cancellationToken);
            }
        }
        finally
        {
            foreach (var pending in _pending.Values) pending.TrySetException(new AcpError(-32000, "The agent closed the connection."));
        }
    }

    /// <summary>
    /// Sends a request and waits for its result.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="parameters">Its parameters.</param>
    /// <param name="cancellationToken">Stops waiting.</param>
    /// <returns>The result.</returns>
    /// <exception cref="AcpError">The agent answered with an error.</exception>
    public async Task<JsonNode?> Request(string method, JsonObject parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId);
        var answer = new TaskCompletionSource<JsonNode?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = answer;
        await Write(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = parameters }, cancellationToken);
        await using var registration = cancellationToken.Register(() => answer.TrySetCanceled(cancellationToken));
        try
        {
            return await answer.Task;
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Sends a notification.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="parameters">Its parameters.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the notification is written.</returns>
    public Task Notify(string method, JsonObject parameters, CancellationToken cancellationToken) =>
        Write(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = parameters }, cancellationToken);

    /// <inheritdoc/>
    public void Dispose() => _writing.Dispose();

    void Dispatch(JsonObject message, CancellationToken cancellationToken)
    {
        var method = message["method"]?.GetValue<string>();
        var id = message["id"];
        if (method is null)
        {
            if (id is JsonValue value && value.TryGetValue<long>(out var number) && _pending.TryGetValue(number, out var pending))
            {
                if (message["error"] is JsonObject error)
                {
                    pending.TrySetException(new AcpError(error["code"]?.GetValue<int>() ?? -32000, error["message"]?.ToString() ?? "The agent answered with an error."));
                }
                else
                {
                    pending.TrySetResult(message["result"]?.DeepClone());
                }
            }

            return;
        }

        if (id is null)
        {
            notify(method, message["params"]);
            return;
        }

        _ = Answer(id.DeepClone(), method, message["params"]?.DeepClone(), cancellationToken);
    }

    async Task Answer(JsonNode id, string method, JsonNode? parameters, CancellationToken cancellationToken)
    {
        JsonObject response;
        try
        {
            var result = await handle(method, parameters, cancellationToken);
            response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };
        }
        catch (AcpError error)
        {
            response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = error.Code, ["message"] = error.Message } };
        }

        await Write(response, cancellationToken);
    }

    async Task Write(JsonObject message, CancellationToken cancellationToken)
    {
        await _writing.WaitAsync(cancellationToken);
        try
        {
            await output.WriteLineAsync(message.ToJsonString());
            await output.FlushAsync(cancellationToken);
        }
        finally
        {
            _writing.Release();
        }
    }
}
