// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Serves the model APIs coding agents speak (OpenAI chat completions and the Anthropic messages API) at an address on this
/// machine, and runs every call through Studio's AI gateway for the slice being built. The organization's providers answer, so
/// Studio counts the tokens, applies its pooling and limits, and no model credential is kept on this machine. The address is
/// reachable only from this machine and needs a random token the agent is given in its environment.
/// </summary>
public sealed class StudioAiBridge : IAsyncDisposable
{
    readonly HttpListener _listener = new();
    readonly StudioBuilderClient _client;
    readonly Action<string> _log;
    readonly CancellationTokenSource _stop = new();
    readonly Task _serving;
    readonly string _token = StudioCredentials.NewToken();
    volatile ActiveBuild? _active;

    StudioAiBridge(StudioBuilderClient client, int port, Action<string> log)
    {
        _client = client;
        _log = log;
        Url = $"http://127.0.0.1:{port}";
        _listener.Prefixes.Add($"{Url}/");
        _listener.Start();
        _serving = Serve();
    }

    /// <summary>Gets the address the bridge is served at.</summary>
    public string Url { get; }

    /// <summary>
    /// Starts the bridge.
    /// </summary>
    /// <param name="client">The connection to Studio.</param>
    /// <param name="log">Where the bridge writes what it is doing.</param>
    /// <returns>The bridge.</returns>
    public static StudioAiBridge Start(StudioBuilderClient client, Action<string> log) => new(client, FreePort(), log);

    /// <summary>
    /// Binds the bridge to the slice being built, so the agent's calls are made for that slice's lease. Passing
    /// <see langword="null"/> ends it: a call made after the build is refused.
    /// </summary>
    /// <param name="sliceId">The slice, or <see langword="null"/>.</param>
    /// <param name="orderId">The order being built.</param>
    public void Bind(string? sliceId, string? orderId) => _active = sliceId is null || orderId is null ? null : new(sliceId, orderId);

    /// <summary>
    /// Gets the environment that points an agent, or a generator that calls a model, at the bridge.
    /// </summary>
    /// <returns>The environment values.</returns>
    public IReadOnlyDictionary<string, string> Environment() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ANTHROPIC_BASE_URL"] = Url,
        ["ANTHROPIC_AUTH_TOKEN"] = _token,
        ["ANTHROPIC_API_KEY"] = _token,
        ["OPENAI_BASE_URL"] = $"{Url}/v1",
        ["OPENAI_API_KEY"] = _token,
        ["STUDIO_AI_URL"] = $"{Url}/v1",
        ["STUDIO_AI_TOKEN"] = _token
    };

    /// <summary>
    /// Gets the token an agent presents, so it can be kept out of what is logged and reported.
    /// </summary>
    /// <returns>The token.</returns>
    public string Token() => _token;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Close();
        try
        {
            await _serving;
        }
        catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or OperationCanceledException)
        {
        }

        _stop.Dispose();
    }

    static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    static JsonObject Error(string message) => new() { ["error"] = new JsonObject { ["message"] = message, ["type"] = "studio_ai_gateway" } };

    static async Task Write(HttpListenerContext context, int status, JsonObject body)
    {
        var bytes = Encoding.UTF8.GetBytes(body.ToJsonString());
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    static async Task Stream(HttpListenerContext context, IEnumerable<(string? Event, string Data)> events)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/event-stream";
        context.Response.SendChunked = true;
        context.Response.Headers["Cache-Control"] = "no-cache";
        await using var writer = new StreamWriter(context.Response.OutputStream, new UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var (name, data) in events)
        {
            if (name is not null) await writer.WriteLineAsync($"event: {name}");
            await writer.WriteLineAsync($"data: {data}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        context.Response.Close();
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

            _ = Task.Run(() => Answer(context), _stop.Token);
        }
    }

    async Task Answer(HttpListenerContext context)
    {
        try
        {
            if (!Authorized(context.Request))
            {
                await Write(context, 401, Error("The request carries no valid token."));
                return;
            }

            var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (context.Request.HttpMethod == "GET" && path == "/v1/models")
            {
                await Write(context, 200, new JsonObject { ["object"] = "list", ["data"] = new JsonArray(new JsonObject { ["id"] = "studio", ["object"] = "model", ["owned_by"] = "studio" }) });
                return;
            }

            if (context.Request.HttpMethod != "POST" || path is not ("/v1/chat/completions" or "/v1/messages"))
            {
                await Write(context, 404, Error("Studio's AI bridge answers /v1/chat/completions and /v1/messages."));
                return;
            }

            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            if (JsonNode.Parse(await reader.ReadToEndAsync(_stop.Token)) is not JsonObject request)
            {
                await Write(context, 400, Error("The request is not a JSON object."));
                return;
            }

            await Complete(context, request, anthropic: path == "/v1/messages");
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or HttpListenerException or IOException or OperationCanceledException)
        {
            _log($"The AI bridge could not answer a call: {exception.Message}");
            try
            {
                await Write(context, 500, Error(exception.Message));
            }
            catch (Exception inner) when (inner is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
            }
        }
    }

    async Task Complete(HttpListenerContext context, JsonObject request, bool anthropic)
    {
        if (_active is not { } active)
        {
            await Write(context, 409, Error("No slice is being built, so there is no lease to make the call under."));
            return;
        }

        var model = (request["model"] as JsonValue)?.TryGetValue<string>(out var asked) == true ? asked : "studio";
        var gateway = anthropic ? StudioAiTranslation.FromAnthropic(request, active.OrderId) : StudioAiTranslation.FromOpenAi(request, active.OrderId);
        var (status, answer) = await _client.Chat(active.SliceId, gateway, _stop.Token);
        if (status != HttpStatusCode.OK)
        {
            var reason = answer["reason"]?.GetValue<string>() ?? $"Studio answered {(int)status}.";
            _log($"Studio's AI gateway refused a call: {reason}");
            await Write(context, (int)status, Error(reason));
            return;
        }

        var wantsStream = request["stream"] is JsonValue stream && stream.TryGetValue<bool>(out var streaming) && streaming;
        if (anthropic)
        {
            var message = StudioAiTranslation.ToAnthropic(answer, model);
            if (wantsStream) await Stream(context, [.. StudioAiTranslation.AnthropicStream(message).Select(entry => ((string?)entry.Event, entry.Data))]);
            else await Write(context, 200, message);
        }
        else
        {
            var completion = StudioAiTranslation.ToOpenAi(answer, model);
            if (wantsStream) await Stream(context, [.. StudioAiTranslation.OpenAiStream(completion).Select(data => ((string?)null, data))]);
            else await Write(context, 200, completion);
        }
    }

    bool Authorized(HttpListenerRequest request)
    {
        var presented = request.Headers["x-api-key"] ?? request.Headers["Authorization"]?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        return presented is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented.Trim()), Encoding.UTF8.GetBytes(_token));
    }

    sealed record ActiveBuild(string SliceId, string OrderId);
}
