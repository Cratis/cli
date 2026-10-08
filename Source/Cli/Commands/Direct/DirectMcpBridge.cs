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
/// Every request is answered exactly once unless the client cancels it: when Direct's reply does not contain the
/// answer (an HTTP error, an empty or unparsable body, an error without the request's id, or an event stream that
/// ends early), the bridge answers with a JSON-RPC error itself. The same holds when forwarding fails unexpectedly and
/// when the bridge stops with requests still in flight, because standard input ended or failed or the bridge was
/// cancelled: each of them is answered with an error before the bridge exits.
/// </remarks>
/// <param name="http">Transport to Direct; must not follow redirects.</param>
/// <param name="tokens">The token provider for the pinned credential.</param>
/// <param name="target">The pinned Direct origin, resource and tenant.</param>
/// <param name="issuer">The pinned issuer that granted the credential.</param>
/// <param name="log">Diagnostic log; standard error, never the protocol stream.</param>
/// <param name="maxMessageLength">The largest message from Direct the bridge holds in memory, in characters; null uses <see cref="MaxMessageLength"/>.</param>
internal sealed class DirectMcpBridge(HttpClient http, IDirectTokenProvider tokens, DirectTarget target, Uri issuer, TextWriter log, int? maxMessageLength = null) : IDisposable
{
    /// <summary>JSON-RPC error code when Direct refuses the stored login.</summary>
    internal const int AuthenticationRequired = -32001;

    /// <summary>JSON-RPC error code when Direct cannot be reached or answers outside the protocol.</summary>
    internal const int TransportFailure = -32000;

    /// <summary>JSON-RPC parse error.</summary>
    internal const int ParseError = -32700;

    /// <summary>JSON-RPC invalid request.</summary>
    internal const int InvalidRequest = -32600;

    /// <summary>JSON-RPC internal error, when forwarding a request fails unexpectedly.</summary>
    internal const int InternalError = -32603;

    /// <summary>The largest message from Direct the bridge holds in memory by default, in characters.</summary>
    internal const int MaxMessageLength = 32 * 1024 * 1024;

    internal const string LoginRequired = "Direct rejected the stored login (HTTP 401) even after refreshing the access token. Run 'cratis direct login', then restart this MCP server.";

    internal const string SessionEnded = "Direct no longer knows this MCP session (HTTP 404); it expired or Direct restarted. Reconnect or restart this MCP server so the client initializes a new session.";

    internal const string BatchesUnsupported = "JSON-RPC batches are not supported; send one message per line.";

    internal const string BridgeStopped = "The Direct MCP bridge stopped before Direct answered the request.";

    internal const string ForwardingFailed = "The Direct MCP bridge failed while forwarding the request; its log on standard error has the details.";

    static readonly MediaTypeWithQualityHeaderValue _json = new("application/json");
    static readonly MediaTypeWithQualityHeaderValue _eventStream = new("text/event-stream");

    readonly SemaphoreSlim _output = new(1, 1);
    readonly SemaphoreSlim _token = new(1, 1);
    readonly ConcurrentDictionary<string, CancellationTokenSource> _inFlight = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, bool> _cancelledByClient = new(StringComparer.Ordinal);
    readonly int _limit = maxMessageLength ?? MaxMessageLength;
    string? _protocolVersion;
    string? _sessionId;

    /// <inheritdoc/>
    public void Dispose()
    {
        _output.Dispose();
        _token.Dispose();
    }

    /// <summary>
    /// Forwards messages until standard input ends, then stops whatever is still in flight and answers each request
    /// among it with an error. Reading standard input failing and cancellation stop the bridge the same way, after
    /// which their exception propagates.
    /// </summary>
    /// <param name="input">The MCP client's messages.</param>
    /// <param name="output">The protocol stream back to the MCP client.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A task completing when every forwarded message has finished.</returns>
    internal async Task Run(TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = new List<Task>();
        try
        {
            while (await input.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                pending.RemoveAll(task => task.IsCompleted);
                pending.Add(Forward(line, output, lifetime.Token));
            }
        }
        finally
        {
            // However the bridge stops, a request still in flight is stopped and answered, never left waiting.
            await lifetime.CancelAsync();
            await Task.WhenAll(pending);
        }
    }

    async Task Forward(string line, TextWriter output, CancellationToken cancellationToken)
    {
        var message = DirectMcpMessage.Parse(line);
        if (message is null)
        {
            await Reject(line, output);
            return;
        }

        if (message.CancelledRequest is { } cancelled && _inFlight.TryGetValue(cancelled, out var cancelling))
        {
            // Recorded before cancelling, so the request knows it was the client, whatever else stops meanwhile.
            _cancelledByClient[cancelled] = true;
            await cancelling.CancelAsync();
        }

        var exchange = new DirectMcpExchange(message);
        using var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tracked = message.IsRequest && _inFlight.TryAdd(message.IdKey!, request);
        try
        {
            await Send(line, exchange, output, request.Token);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            // MCP expects no response to a request the client cancelled, but the bridge stopping is not the client's
            // doing. Who cancelled is recorded, not inferred: the bridge may start stopping while a request the client
            // cancelled is still unwinding.
            var cancelledByClient = tracked && _cancelledByClient.ContainsKey(message.IdKey!);
            if (!cancelledByClient && cancellationToken.IsCancellationRequested)
            {
                await Stopped(output, exchange);
            }
        }
        catch (DirectAuthError ex)
        {
            await Fail(output, exchange, AuthenticationRequired, ex.Message);
        }
        catch (DirectMcpMessageTooLarge ex)
        {
            await Fail(output, exchange, TransportFailure, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            await Fail(output, exchange, TransportFailure, "Direct's MCP endpoint could not be reached or did not answer in time.");
        }
        catch (Exception ex)
        {
            // Anything else is a defect, not a reason to leave the client waiting for an answer that never comes.
            await log.WriteLineAsync($"Direct MCP: forwarding failed unexpectedly: {ex.GetType().Name}: {ex.Message}");
            await Fail(output, exchange, InternalError, ForwardingFailed);
        }
        finally
        {
            if (tracked)
            {
                _inFlight.TryRemove(message.IdKey!, out _);
                _cancelledByClient.TryRemove(message.IdKey!, out _);
            }
        }
    }

    async Task Stopped(TextWriter output, DirectMcpExchange exchange)
    {
        try
        {
            await Fail(output, exchange, TransportFailure, BridgeStopped);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // A client that closed its end first cannot read the answer; that is no reason to fail the shutdown.
            await log.WriteLineAsync($"Direct MCP: could not answer a request while stopping: {ex.Message}");
        }
    }

    async Task Reject(string line, TextWriter output)
    {
        if (DirectMcpMessage.BatchAnswers(line) is { } ids)
        {
            // MCP removed JSON-RPC batching; answering each request keeps a client that still batches from waiting.
            await log.WriteLineAsync($"Direct MCP: {BatchesUnsupported}");
            foreach (var id in ids)
            {
                await Write(output, DirectMcpExchange.Error(id, InvalidRequest, BatchesUnsupported));
            }

            return;
        }

        await Write(output, DirectMcpExchange.Error(null, ParseError, "Parse error: the message is not a JSON-RPC object."));
    }

    async Task Send(string line, DirectMcpExchange exchange, TextWriter output, CancellationToken cancellationToken)
    {
        // A new initialize starts a new session, so it never carries the previous session's headers.
        var initializing = exchange.Message.Method == "initialize";
        var session = initializing ? null : Volatile.Read(ref _sessionId);
        var token = await AccessToken(null, cancellationToken);
        var response = await Post(line, token, initializing, session, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await log.WriteLineAsync("Direct MCP: Direct rejected the access token; refreshing it once.");
            token = await AccessToken(token, cancellationToken);
            response = await Post(line, token, initializing, session, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                await Fail(output, exchange, AuthenticationRequired, LoginRequired);
                return;
            }
        }

        using (response)
        {
            await Relay(response, exchange, session, output, cancellationToken);
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

    Task<HttpResponseMessage> Post(string line, string token, bool initializing, string? session, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, target.Resource)
        {
            Content = new StringContent(line, new UTF8Encoding(false), "application/json")
        };
        request.Headers.Accept.Add(_json);
        request.Headers.Accept.Add(_eventStream);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!initializing && Volatile.Read(ref _protocolVersion) is { } version)
        {
            request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", version);
        }

        if (session is not null)
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

    async Task Relay(HttpResponseMessage response, DirectMcpExchange exchange, string? sentSession, TextWriter output, CancellationToken cancellationToken)
    {
        var session = response.Headers.TryGetValues("Mcp-Session-Id", out var sessions) && sessions.FirstOrDefault() is { Length: > 0 } issued ? issued : null;
        if (session is not null || (exchange.Message.Method == "initialize" && response.IsSuccessStatusCode))
        {
            Volatile.Write(ref _sessionId, session);
        }

        if (!response.IsSuccessStatusCode)
        {
            await RelayFailure(response, exchange, sentSession, output, cancellationToken);
            return;
        }

        if (string.Equals(response.Content.Headers.ContentType?.MediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await foreach (var data in DirectMcpServerSentEvents.Read(new DirectMcpBoundedReader(reader, _limit), cancellationToken))
            {
                await Relay(data, exchange, output);
            }
        }
        else if (response.StatusCode is not (HttpStatusCode.Accepted or HttpStatusCode.NoContent))
        {
            await Relay(await Body(response, cancellationToken), exchange, output);
        }

        if (exchange.Message.IsRequest)
        {
            var (code, text) = exchange.StrayError ?? (TransportFailure, "Direct's MCP endpoint ended the response without answering the request.");
            await Fail(output, exchange, code, text);
        }
    }

    async Task<string> Body(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await new DirectMcpBoundedReader(reader, _limit).ReadToEnd(cancellationToken);
    }

    async Task Relay(string body, DirectMcpExchange exchange, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            await log.WriteLineAsync("Direct MCP: ignored a response from Direct that is not JSON.");
            return;
        }

        foreach (var message in node is JsonArray batch ? [.. batch] : new[] { node })
        {
            await RelayMessage(message, exchange, output);
        }
    }

    async Task RelayMessage(JsonNode? node, DirectMcpExchange exchange, TextWriter output)
    {
        var (relay, ignored) = exchange.Receive(node);
        if (relay is null)
        {
            await log.WriteLineAsync($"Direct MCP: {ignored}");
            return;
        }

        if (exchange.Message.Method == "initialize" && !relay.ContainsKey("method") && relay["result"] is JsonObject result && result["protocolVersion"] is JsonValue version && version.TryGetValue<string>(out var negotiated))
        {
            Volatile.Write(ref _protocolVersion, negotiated);
        }

        await Write(output, relay);
    }

    async Task RelayFailure(HttpResponseMessage response, DirectMcpExchange exchange, string? sentSession, TextWriter output, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        if (response.StatusCode == HttpStatusCode.NotFound && sentSession is not null)
        {
            // The client, not the bridge, must initialize again; the next initialize is sent without the dead session.
            Interlocked.CompareExchange(ref _sessionId, null, sentSession);
            await Fail(output, exchange, TransportFailure, SessionEnded);
            return;
        }

        if (string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            await Relay(await Body(response, cancellationToken), exchange, output);
        }

        var text = exchange.StrayError is { } error
            ? $"{error.Text} (HTTP {status})"
            : response.StatusCode switch
            {
                HttpStatusCode.Forbidden => "Direct refused the request (HTTP 403): the login may lack the required scope or access to this tenant.",
                _ when status is >= 300 and < 400 => $"Direct redirected the MCP request (HTTP {status}); the bridge does not follow redirects with credentials. Check --url.",
                _ => $"Direct's MCP endpoint returned HTTP {status}."
            };
        await Fail(output, exchange, exchange.StrayError?.Code ?? TransportFailure, text);
    }

    async Task Fail(TextWriter output, DirectMcpExchange exchange, int code, string text)
    {
        if (exchange.Answered)
        {
            return;
        }

        await log.WriteLineAsync($"Direct MCP: {text}");
        if (exchange.Fail(code, text) is { } error)
        {
            await Write(output, error);
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
