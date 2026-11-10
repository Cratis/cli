// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Serves a local MCP server that speaks over standard input and output at an address on this machine, for an agent that only
/// connects to MCP servers over HTTP (Copilot CLI refuses every stdio server a client hands it). Each request is answered with
/// one JSON response; the address is reachable only from this machine and needs a random token the agent is given in the session.
/// </summary>
public sealed class McpHttpBridge : IAsyncDisposable
{
    readonly HttpListener _listener = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _waiting = new();
    readonly SemaphoreSlim _writing = new(1, 1);
    readonly CancellationTokenSource _stop = new();
    readonly Process _server;
    readonly string _token;
    readonly Task _serving;
    readonly Task _reading;

    McpHttpBridge(Process server, int port, string token)
    {
        _server = server;
        _token = token;
        Url = $"http://127.0.0.1:{port}/mcp/";
        _listener.Prefixes.Add(Url);
        _listener.Start();
        _reading = Read();
        _serving = Serve();
    }

    /// <summary>Gets the address the server is served at.</summary>
    public string Url { get; }

    /// <summary>
    /// Starts the local server and serves it.
    /// </summary>
    /// <param name="command">The command that starts the server.</param>
    /// <param name="arguments">Its arguments.</param>
    /// <param name="workingDirectory">Where it runs.</param>
    /// <returns>The bridge.</returns>
    /// <exception cref="InvalidOperationException">The server could not be started.</exception>
    public static McpHttpBridge Start(string command, IReadOnlyList<string> arguments, string workingDirectory)
    {
        var start = new ProcessStartInfo(command)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        var server = Process.Start(start) ?? throw new InvalidOperationException($"{command} could not be started.");
        server.ErrorDataReceived += (_, _) => { };
        server.BeginErrorReadLine();
        return new(server, FreePort(), Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
    }

    /// <summary>
    /// Gets the identifier of a JSON-RPC message as text, or <see langword="null"/> for a notification.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The identifier.</returns>
    public static string? IdOf(JsonNode? message) => message?["id"] is JsonValue id ? id.ToJsonString() : null;

    /// <summary>
    /// Describes the bridged server for a session: the same name and tools, reached over HTTP with the bridge's token.
    /// </summary>
    /// <param name="tool">The local server.</param>
    /// <returns>The server as the agent reaches it.</returns>
    public BuildAgentTool Describe(BuildAgentTool tool) => tool with { Command = null, Arguments = [], Url = Url, BearerToken = _token };

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Close();
        try
        {
            _server.StandardInput.Close();
            using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _server.WaitForExitAsync(grace.Token);
        }
        catch (OperationCanceledException)
        {
            _server.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The server already exited.
        }

        foreach (var waiting in _waiting.Values) waiting.TrySetCanceled();
        await Task.WhenAll(_serving, _reading).ContinueWith(_ => { }, TaskScheduler.Default);
        _server.Dispose();
        _writing.Dispose();
        _stop.Dispose();
    }

    static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    async Task Read()
    {
        while (await _server.StandardOutput.ReadLineAsync(_stop.Token).AsTask().ContinueWith(task => task.IsCompletedSuccessfully ? task.Result : null, TaskScheduler.Default) is { } line)
        {
            JsonNode? message;
            try
            {
                message = JsonNode.Parse(line);
            }
            catch (System.Text.Json.JsonException)
            {
                continue;
            }

            // Only responses go back; what the server sends on its own (logging, progress) has no request waiting for it.
            if (message?["method"] is null && IdOf(message) is { } id && _waiting.TryRemove(id, out var waiting)) waiting.TrySetResult(line);
        }

        foreach (var waiting in _waiting.Values) waiting.TrySetException(new IOException("The MCP server stopped."));
    }

    async Task Serve()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            _ = Answer(context);
        }
    }

    async Task Answer(HttpListenerContext context)
    {
        var response = context.Response;
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(context.Request.Headers["Authorization"] ?? string.Empty), Encoding.UTF8.GetBytes($"Bearer {_token}")))
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (context.Request.HttpMethod == "DELETE")
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }

            if (context.Request.HttpMethod != "POST")
            {
                // No stream of messages from the server: every answer comes with its request.
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return;
            }

            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(_stop.Token);
            var answer = await Forward(body);
            if (answer is null)
            {
                response.StatusCode = (int)HttpStatusCode.Accepted;
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(answer);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(bytes, _stop.Token);
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or System.Text.Json.JsonException or HttpListenerException)
        {
            response.StatusCode = (int)HttpStatusCode.BadGateway;
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException)
            {
                // The agent went away.
            }
        }
    }

    async Task<string?> Forward(string body)
    {
        var node = JsonNode.Parse(body);
        var messages = node is JsonArray batch ? [.. batch] : new List<JsonNode?> { node };
        var answers = new List<Task<string>>();
        foreach (var message in messages)
        {
            if (IdOf(message) is { } id && message?["method"] is not null)
            {
                var waiting = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiting[id] = waiting;
                answers.Add(waiting.Task);
            }

            await _writing.WaitAsync(_stop.Token);
            try
            {
                await _server.StandardInput.WriteLineAsync(message!.ToJsonString().AsMemory(), _stop.Token);
                await _server.StandardInput.FlushAsync(_stop.Token);
            }
            finally
            {
                _writing.Release();
            }
        }

        if (answers.Count == 0) return null;
        var lines = await Task.WhenAll(answers);
        return node is JsonArray ? $"[{string.Join(',', lines)}]" : lines[0];
    }
}
