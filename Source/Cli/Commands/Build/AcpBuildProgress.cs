// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Projects ACP session/update tool calls into activity only. See
/// https://agentclientprotocol.com/protocol/tool-calls and /protocol/session-update.
/// Content blocks, raw tool inputs and raw outputs are deliberately not inspected.
/// </summary>
public static class AcpBuildProgress
{
    static readonly HashSet<string> _statuses = [with(StringComparer.Ordinal), "pending", "in_progress", "completed", "failed"];

    /// <summary>Publishes a supported tool-call update for the active session.</summary>
    /// <param name="sessionId">The active session.</param>
    /// <param name="parameters">Notification parameters.</param>
    /// <param name="progress">The nonblocking progress sink.</param>
    public static void Publish(string? sessionId, JsonNode? parameters, Action<BuildProgress> progress)
    {
        if (parameters is not JsonObject notification || sessionId is null || Text(notification["sessionId"]) != sessionId) return;
        var update = notification["update"] as JsonObject;
        var type = Text(update?["sessionUpdate"]);
        if (type is not ("tool_call" or "tool_call_update")) return;
        if (string.IsNullOrWhiteSpace(Text(update?["toolCallId"]))) return;
        var title = Text(update?["title"]);
        if (string.IsNullOrWhiteSpace(title)) return;
        progress(new(Step(update, title)));
    }

    static string Step(JsonObject? update, string title)
    {
        var status = Text(update?["status"]);
        return _statuses.Contains(status ?? string.Empty) ? $"{title} ({status})" : title;
    }

    static string? Text(JsonNode? value) => value is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;
}
