// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Answers an agent's <c language="csharp">session/request_permission</c> from the session's permissions: reading the project and the
/// folders handed over, writing the paths the kit permits, running the commands it permits, and the session's tools.
/// Fetching from the web, switching modes and anything the host cannot classify are refused.
/// </summary>
public static class AcpPermissionDecision
{
    static readonly string[] _readKinds = ["read", "search", "think"];
    static readonly string[] _writeKinds = ["edit", "delete", "move"];

    /// <summary>
    /// Decides whether to allow a tool call.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="toolCall">The tool call the agent asks about.</param>
    /// <returns><see langword="true"/> to allow it.</returns>
    public static bool Allows(BuildAgentSession session, JsonNode? toolCall)
    {
        var kind = Text(toolCall?["kind"]) ?? "other";
        var paths = Paths(toolCall).ToList();
        var permissions = session.Permissions;

        if (_readKinds.Contains(kind)) return paths.TrueForAll(permissions.AllowsRead);
        if (_writeKinds.Contains(kind)) return paths.Count > 0 && paths.TrueForAll(permissions.AllowsWrite);
        if (kind == "execute") return Command(toolCall) is { } command && permissions.AllowsCommand(command);
        if (string.Equals(kind, "fetch", StringComparison.Ordinal) || string.Equals(kind, "switch_mode", StringComparison.Ordinal)) return false;
        return AllowsTool(session, toolCall);
    }

    /// <summary>
    /// Composes the answer to a permission request.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="parameters">The request's parameters.</param>
    /// <returns>The outcome: the allowing or refusing option, or cancelled when the agent offers none.</returns>
    public static JsonObject Answer(BuildAgentSession session, JsonNode? parameters)
    {
        var allowed = Allows(session, parameters?["toolCall"]);
        var options = (parameters?["options"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var preferred = allowed ? new[] { "allow_once", "allow_always" } : ["reject_once", "reject_always"];
        var option = preferred.Select(kind => options.FirstOrDefault(candidate => Text(candidate["kind"]) == kind)).FirstOrDefault(candidate => candidate is not null);
        if (!allowed) session.Log($"Refused the agent's request: {Text(parameters?["toolCall"]?["title"]) ?? "an action the build does not permit"}.");
        return option is null
            ? new JsonObject { ["outcome"] = new JsonObject { ["outcome"] = "cancelled" } }
            : new JsonObject { ["outcome"] = new JsonObject { ["outcome"] = "selected", ["optionId"] = Text(option["optionId"]) } };
    }

    static bool AllowsTool(BuildAgentSession session, JsonNode? toolCall)
    {
        var described = $"{Text(toolCall?["title"])} {toolCall?["rawInput"]?.ToJsonString()}";
        foreach (var tool in session.Tools)
        {
            if (tool.ReadOnlyTools.Any(name => described.Contains(name, StringComparison.Ordinal))) return true;
            if (session.MayChangeTools && (described.Contains($"mcp__{tool.Name}", StringComparison.Ordinal) || described.Contains($"{tool.Name}_", StringComparison.Ordinal) || described.Contains($"{tool.Name}/", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    static IEnumerable<string> Paths(JsonNode? toolCall)
    {
        foreach (var location in (toolCall?["locations"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (Text(location["path"]) is { } path) yield return path;
        }

        var raw = toolCall?["rawInput"] as JsonObject;
        foreach (var member in new[] { "path", "file_path", "filePath", "abs_path", "destination" })
        {
            if (Text(raw?[member]) is { } path) yield return path;
        }
    }

    static string? Command(JsonNode? toolCall)
    {
        var raw = toolCall?["rawInput"] as JsonObject;
        return raw?["command"] switch
        {
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            JsonArray parts => string.Join(' ', parts.Select(part => part?.ToString())),
            _ => Text(raw?["cmd"])
        };
    }

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
