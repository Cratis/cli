// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// One client message on its way through the bridge: decides which of the messages Direct sends back reach the client,
/// and makes sure a request is answered exactly once.
/// </summary>
/// <param name="message">The client message.</param>
internal sealed class DirectMcpExchange(DirectMcpMessage message)
{
    JsonObject? _strayError;

    /// <summary>Gets the client message.</summary>
    internal DirectMcpMessage Message => message;

    /// <summary>Gets whether the client's request has been answered.</summary>
    internal bool Answered { get; private set; }

    /// <summary>Gets the error of a response Direct sent that did not answer the request, to explain the bridge's own answer.</summary>
    internal (int Code, string Text)? StrayError => Describe(_strayError);

    /// <summary>Creates a JSON-RPC error response.</summary>
    /// <param name="id">The id of the request it answers; null when it answers none.</param>
    /// <param name="code">The error code.</param>
    /// <param name="text">The error message.</param>
    /// <returns>The response.</returns>
    internal static JsonObject Error(JsonNode? id, int code, string text) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = code, ["message"] = text }
    };

    /// <summary>
    /// Decides what reaches the client from one message Direct sent: its requests and notifications, and the one response
    /// that answers the pending request. A response the client did not ask for, such as an error without an id, would
    /// never be matched by the client, so it is held back and its error kept.
    /// </summary>
    /// <param name="node">The message from Direct.</param>
    /// <returns>The message to write to the client, or null with the reason it was held back.</returns>
    internal (JsonObject? Relay, string? Ignored) Receive(JsonNode? node)
    {
        if (node is not JsonObject received)
        {
            return (null, "ignored a response from Direct that is not a JSON-RPC message.");
        }

        if (received.ContainsKey("method"))
        {
            return (received, null);
        }

        if (message.IsRequest && !Answered && received["id"] is { } id && DirectMcpMessage.KeyOf(id) == message.IdKey)
        {
            if (message.Method == "initialize" && !received.ContainsKey("error") &&
                (received["result"] is not JsonObject result || result["protocolVersion"] is not JsonValue version ||
                 !version.TryGetValue<string>(out var protocolVersion) || string.IsNullOrWhiteSpace(protocolVersion)))
            {
                return (Fail(DirectMcpBridge.TransportFailure, "Direct returned an invalid initialize result without a protocol version."), null);
            }

            Answered = true;
            return (received, null);
        }

        _strayError ??= received["error"] as JsonObject;
        return (null, $"ignored a response from Direct that does not answer the pending request{(Describe(received["error"] as JsonObject) is { } error ? $": {error.Text}" : ".")}");
    }

    /// <summary>Claims the answer to the request for an error the bridge sends itself.</summary>
    /// <param name="code">The JSON-RPC error code.</param>
    /// <param name="text">The error message.</param>
    /// <returns>The error response, or null when the message is not a request or it has been answered.</returns>
    internal JsonObject? Fail(int code, string text)
    {
        if (!message.IsRequest || Answered)
        {
            return null;
        }

        Answered = true;
        return Error(message.Id, code, text);
    }

    static (int Code, string Text)? Describe(JsonObject? error) =>
        error?["message"] is JsonValue text && text.TryGetValue<string>(out var message)
            ? (error["code"] is JsonValue value && value.TryGetValue<int>(out var code) ? code : DirectMcpBridge.TransportFailure, message)
            : null;
}
