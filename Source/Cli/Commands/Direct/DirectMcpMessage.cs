// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Direct;

/// <summary>The routing facts the bridge needs from one client message; the message itself is forwarded unchanged.</summary>
/// <param name="Id">The JSON-RPC id, or null for a notification.</param>
/// <param name="Method">The method, or null for a response to a server request.</param>
/// <param name="CancelledRequest">The id key of the request a 'notifications/cancelled' message cancels.</param>
internal sealed record DirectMcpMessage(JsonNode? Id, string? Method, string? CancelledRequest)
{
    /// <summary>Gets whether the client expects a response.</summary>
    internal bool IsRequest => Id is not null && Method is not null;

    /// <summary>Gets a comparable key for the id.</summary>
    internal string? IdKey => Id is null ? null : KeyOf(Id);

    /// <summary>Reads the routing facts from a line, or null when it is not a JSON object.</summary>
    /// <param name="line">The client message.</param>
    /// <returns>The routing facts.</returns>
    internal static DirectMcpMessage? Parse(string line)
    {
        try
        {
            if (JsonNode.Parse(line) is not JsonObject message)
            {
                return null;
            }

            var method = message["method"] is JsonValue value && value.TryGetValue<string>(out var name) ? name : null;
            var cancelled = method == "notifications/cancelled" && message["params"]?["requestId"] is { } requestId ? KeyOf(requestId) : null;
            return new(message["id"]?.DeepClone(), method, cancelled);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads which answers a JSON-RPC batch expects: the id of each request, and null for each entry that is not a
    /// message at all. Notifications expect nothing; an empty batch expects one error without an id.
    /// </summary>
    /// <param name="line">The client message.</param>
    /// <returns>The ids to answer, or null when the line is not a batch.</returns>
    internal static IReadOnlyList<JsonNode?>? BatchAnswers(string line)
    {
        try
        {
            if (JsonNode.Parse(line) is not JsonArray batch)
            {
                return null;
            }

            if (batch.Count == 0)
            {
                return [null];
            }

            var answers = new List<JsonNode?>();
            foreach (var entry in batch)
            {
                if (entry is not JsonObject message)
                {
                    answers.Add(null);
                }
                else if (message["id"] is { } id && message.ContainsKey("method"))
                {
                    answers.Add(id.DeepClone());
                }
            }

            return answers;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Distinguishes the string id "1" from the number 1, as JSON-RPC does.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The key.</returns>
    internal static string KeyOf(JsonNode id) => id.ToJsonString();
}
