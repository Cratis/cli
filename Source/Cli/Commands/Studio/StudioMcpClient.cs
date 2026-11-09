// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Studio;

/// <summary>
/// Calls the tools of Studio's MCP server with the organization's MCP credential: exchanges the client credentials for a
/// bearer token, opens an MCP session over streamable HTTP, and calls tools by name.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="server">The MCP server's address, such as https://mcp.cratis.studio.</param>
/// <param name="clientId">The organization's client id, its namespace.</param>
/// <param name="clientSecret">The organization's client secret.</param>
public sealed class StudioMcpClient(HttpClient http, Uri server, string clientId, string clientSecret)
{
    const string ProtocolVersion = "2025-06-18";

    string? _token;
    string? _session;
    long _nextId;

    /// <summary>
    /// Gets a bearer token for the organization, exchanging the client credentials once.
    /// </summary>
    /// <param name="cancellationToken">Cancels the exchange.</param>
    /// <returns>The token.</returns>
    /// <exception cref="StudioMcpError">The credentials were refused.</exception>
    public async Task<string> Token(CancellationToken cancellationToken)
    {
        if (_token is not null) return _token;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });
        using var response = await http.PostAsync(new Uri(server, "/.cratis/token"), content, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new StudioMcpError($"Studio refused the MCP credential ({(int)response.StatusCode}). Check the client id and secret under Settings > MCP.");
        var answer = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        _token = answer?["access_token"]?.GetValue<string>() ?? throw new StudioMcpError("Studio's token answer holds no access token.");
        return _token;
    }

    /// <summary>
    /// Calls a tool and reads its structured answer.
    /// </summary>
    /// <param name="name">The tool's name.</param>
    /// <param name="arguments">Its arguments.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>What the tool answered, as JSON when it answered JSON, else as a JSON string.</returns>
    /// <exception cref="StudioMcpError">The server or the tool answered with an error.</exception>
    public async Task<JsonNode?> Call(string name, JsonObject arguments, CancellationToken cancellationToken)
    {
        await Open(cancellationToken);
        var result = await Request("tools/call", new JsonObject { ["name"] = name, ["arguments"] = arguments }, cancellationToken);
        var text = string.Concat((result?["content"] as JsonArray ?? []).Select(block => block?["text"]?.GetValue<string>()));
        if (result?["isError"] is JsonValue isError && isError.TryGetValue<bool>(out var failed) && failed)
        {
            throw new StudioMcpError(text.Length > 0 ? text : $"The tool {name} failed.");
        }

        if (result?["structuredContent"] is JsonNode structured) return structured.DeepClone();
        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return JsonValue.Create(text);
        }
    }

    static JsonNode? FromEvents(string body, long id)
    {
        foreach (var data in body.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var json = string.Join('\n', data.Split('\n').Where(line => line.StartsWith("data:", StringComparison.Ordinal)).Select(line => line[5..].TrimStart()));
            if (json.Length == 0) continue;
            var message = JsonNode.Parse(json);
            if (message?["id"] is JsonValue value && value.TryGetValue<long>(out var answered) && answered == id) return message;
        }

        return null;
    }

    async Task Open(CancellationToken cancellationToken)
    {
        if (_session is not null) return;
        var initialize = new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject { ["name"] = "cratis-cli", ["version"] = typeof(StudioMcpClient).Assembly.GetName().Version?.ToString() ?? "0" }
        };
        await Request("initialize", initialize, cancellationToken);
        await Send(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "notifications/initialized" }, cancellationToken);
        _session ??= string.Empty;
    }

    async Task<JsonNode?> Request(string method, JsonObject parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId);
        var answer = await Send(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = parameters }, cancellationToken);
        if (answer?["error"] is JsonObject error) throw new StudioMcpError(error["message"]?.ToString() ?? $"Studio's MCP server refused {method}.");
        return answer?["result"];
    }

    async Task<JsonNode?> Send(JsonObject message, CancellationToken cancellationToken, bool renewed = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(server, "/mcp"))
        {
            Content = new StringContent(message.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token(cancellationToken));
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", ProtocolVersion);
        if (!string.IsNullOrEmpty(_session)) request.Headers.Add("Mcp-Session-Id", _session);

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !renewed)
        {
            _token = null;
            return await Send(message, cancellationToken, renewed: true);
        }

        if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessions)) _session = sessions.FirstOrDefault() ?? _session;
        if (!response.IsSuccessStatusCode) throw new StudioMcpError($"Studio's MCP server answered {(int)response.StatusCode} {response.ReasonPhrase}.");
        if (message["id"] is null) return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.Content.Headers.ContentType?.MediaType == "text/event-stream" ? FromEvents(body, message["id"]!.GetValue<long>()) : JsonNode.Parse(body);
    }
}
