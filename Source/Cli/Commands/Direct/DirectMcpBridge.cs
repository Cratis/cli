// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// Forwards newline-delimited JSON-RPC from a stdio MCP client to Direct's Streamable HTTP endpoint, and relays JSON
/// or Server-Sent Events responses back as single lines. The bridge holds no tool definitions; they stay in Direct.
/// </summary>
/// <remarks>
/// Origin, resource, tenant and credential are fixed when the bridge is created. Every request asks the token provider
/// for a current access token; a 401 triggers one refresh and one retry, after which the client receives a JSON-RPC
/// error telling the user to log in again. Tokens are only ever written to the Authorization header.
/// </remarks>
/// <param name="http">Transport to Direct; must not follow redirects.</param>
/// <param name="tokens">The token provider for the pinned credential.</param>
/// <param name="target">The pinned Direct origin, resource and tenant.</param>
/// <param name="issuer">The pinned issuer that granted the credential.</param>
/// <param name="log">Diagnostic log; standard error, never the protocol stream.</param>
internal sealed class DirectMcpBridge(HttpClient http, IDirectTokenProvider tokens, DirectTarget target, Uri issuer, TextWriter log) : IDisposable
{
    /// <summary>JSON-RPC error code when Direct refuses the stored login.</summary>
    internal const int AuthenticationRequired = -32001;

    /// <summary>JSON-RPC error code when Direct cannot be reached or answers outside the protocol.</summary>
    internal const int TransportFailure = -32000;

    /// <summary>JSON-RPC parse error.</summary>
    internal const int ParseError = -32700;

    internal const string LoginRequired = "Direct rejected the stored login (HTTP 401) even after refreshing the access token. Run 'cratis direct login', then restart this MCP server.";

    static readonly MediaTypeWithQualityHeaderValue _json = new("application/json");
    static readonly MediaTypeWithQualityHeaderValue _eventStream = new("text/event-stream");

    readonly SemaphoreSlim _output = new(1, 1);
    readonly SemaphoreSlim _token = new(1, 1);
    readonly ConcurrentDictionary<string, CancellationTokenSource> _inFlight = new(StringComparer.Ordinal);
    string? _protocolVersion;
    string? _sessionId;

    /// <inheritdoc/>
    public void Dispose()
    {
        _output.Dispose();
        _token.Dispose();
    }

    /// <summary>Forwards messages until standard input ends, then cancels whatever is still in flight.</summary>
    /// <param name="input">The MCP client's messages.</param>
    /// <param name="output">The protocol stream back to the MCP client.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A task completing when every forwarded message has finished.</returns>
    internal async Task Run(TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = new List<Task>();
        while (await input.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            pending.RemoveAll(task => task.IsCompleted);
            pending.Add(Forward(line, output, lifetime.Token));
        }

        // The client closed its end: nobody is left to read answers to requests still in flight.
        await lifetime.CancelAsync();
        await Task.WhenAll(pending);
    }

    static bool Answers(JsonNode node, string idKey) => node is JsonObject response &&
        !response.ContainsKey("method") && response["id"] is { } id && DirectMcpMessage.KeyOf(id) == idKey;

    static JsonObject Error(JsonNode? id, int code, string text) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = code, ["message"] = text }
    };

    async Task Forward(string line, TextWriter output, CancellationToken cancellationToken)
    {
        var message = DirectMcpMessage.Parse(line);
        if (message is null)
        {
            await Write(output, Error(null, ParseError, "Parse error: the message is not a JSON-RPC object."));
            return;
        }

        if (message.CancelledRequest is { } cancelled && _inFlight.TryGetValue(cancelled, out var cancelling))
        {
            await cancelling.CancelAsync();
        }

        using var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tracked = message.IsRequest && _inFlight.TryAdd(message.IdKey!, request);
        try
        {
            await Send(line, message, output, request.Token);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            // Cancelled by the client or at shutdown; MCP expects no response to a cancelled request.
        }
        catch (DirectAuthError ex)
        {
            await Fail(output, message, AuthenticationRequired, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            await Fail(output, message, TransportFailure, "Direct's MCP endpoint could not be reached or did not answer in time.");
        }
        finally
        {
            if (tracked)
            {
                _inFlight.TryRemove(message.IdKey!, out _);
            }
        }
    }

    async Task Send(string line, DirectMcpMessage message, TextWriter output, CancellationToken cancellationToken)
    {
        var token = await AccessToken(null, cancellationToken);
        var response = await Post(line, token, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await log.WriteLineAsync("Direct MCP: Direct rejected the access token; refreshing it once.");
            token = await AccessToken(token, cancellationToken);
            response = await Post(line, token, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                await Fail(output, message, AuthenticationRequired, LoginRequired);
                return;
            }
        }

        using (response)
        {
            await Relay(response, message, output, cancellationToken);
        }
    }

    async Task<string> AccessToken(string? rejected, CancellationToken cancellationToken)
    {
        // One acquisition at a time: concurrent requests share a refresh instead of racing the credential store.
        await _token.WaitAsync(cancellationToken);
        try
        {
            return rejected is null
                ? await tokens.GetAccessToken(target, issuer, cancellationToken)
                : await tokens.RefreshAccessToken(target, issuer, rejected, cancellationToken);
        }
        finally
        {
            _token.Release();
        }
    }

    Task<HttpResponseMessage> Post(string line, string token, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, target.Resource)
        {
            Content = new StringContent(line, new UTF8Encoding(false), "application/json")
        };
        request.Headers.Accept.Add(_json);
        request.Headers.Accept.Add(_eventStream);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (Volatile.Read(ref _protocolVersion) is { } version)
        {
            request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", version);
        }

        if (Volatile.Read(ref _sessionId) is { } session)
        {
            request.Headers.TryAddWithoutValidation("Mcp-Session-Id", session);
        }

        return SendDisposing(request, cancellationToken);
    }

    async Task<HttpResponseMessage> SendDisposing(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }

    async Task Relay(HttpResponseMessage response, DirectMcpMessage message, TextWriter output, CancellationToken cancellationToken)
    {
        if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessions) && sessions.FirstOrDefault() is { Length: > 0 } session)
        {
            Volatile.Write(ref _sessionId, session);
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!response.IsSuccessStatusCode)
        {
            await RelayFailure(response, message, mediaType, output, cancellationToken);
            return;
        }

        var answered = false;
        if (string.Equals(mediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await foreach (var data in DirectMcpServerSentEvents.Read(reader, cancellationToken))
            {
                answered |= await Relay(data, message, output);
            }
        }
        else if (response.StatusCode is not (HttpStatusCode.Accepted or HttpStatusCode.NoContent))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                answered = await Relay(body, message, output);
            }
        }

        if (message.IsRequest && !answered)
        {
            await Fail(output, message, TransportFailure, "Direct's MCP endpoint ended the response without answering the request.");
        }
    }

    async Task<bool> Relay(string body, DirectMcpMessage message, TextWriter output)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            await log.WriteLineAsync("Direct MCP: ignored a response from Direct that is not JSON.");
            return false;
        }

        if (node is null)
        {
            return false;
        }

        var answers = message.IsRequest && Answers(node, message.IdKey!);
        if (answers && message.Method == "initialize" && node["result"]?["protocolVersion"] is JsonValue version && version.TryGetValue<string>(out var negotiated))
        {
            Volatile.Write(ref _protocolVersion, negotiated);
        }

        await Write(output, node);
        return answers;
    }

    async Task RelayFailure(HttpResponseMessage response, DirectMcpMessage message, string? mediaType, TextWriter output, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        if (string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken)) is JsonObject error && error.ContainsKey("jsonrpc"))
                {
                    await Write(output, error);
                    return;
                }
            }
            catch (JsonException)
            {
                // Not a protocol error body; report the status instead.
            }
        }

        var text = response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "Direct refused the request (HTTP 403): the login may lack the required scope or access to this tenant.",
            _ when status is >= 300 and < 400 => $"Direct redirected the MCP request (HTTP {status}); the bridge does not follow redirects with credentials. Check --url.",
            _ => $"Direct's MCP endpoint returned HTTP {status}."
        };
        await Fail(output, message, TransportFailure, text);
    }

    async Task Fail(TextWriter output, DirectMcpMessage message, int code, string text)
    {
        await log.WriteLineAsync($"Direct MCP: {text}");
        if (message.IsRequest)
        {
            await Write(output, Error(message.Id, code, text));
        }
    }

    async Task Write(TextWriter output, JsonNode message)
    {
        var line = message.ToJsonString();
        await _output.WaitAsync();
        try
        {
            await output.WriteAsync(line);
            await output.WriteAsync('\n');
            await output.FlushAsync();
        }
        finally
        {
            _output.Release();
        }
    }
}
