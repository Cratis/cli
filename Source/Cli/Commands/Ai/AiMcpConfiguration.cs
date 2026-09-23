// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// A project-owned choice for an optional corpus MCP server.
/// </summary>
/// <param name="Enabled">Whether to register the server for selected profiles.</param>
/// <param name="Root">An optional portable project-relative model directory.</param>
public sealed record AiMcpConfiguration(bool Enabled = true, string? Root = null)
{
    internal static IReadOnlyDictionary<string, AiMcpConfiguration>? Read(JsonElement document)
    {
        if (document.EnumerateObject().GroupBy(property => property.Name, StringComparer.Ordinal).Any(properties => properties.Count() > 1)) throw new AiMcpConfigurationInvalid("Duplicate project configuration property.");
        if (!document.TryGetProperty("mcpServers", out var servers)) return null;
        if (servers.ValueKind != JsonValueKind.Object) throw new AiMcpConfigurationInvalid("mcpServers must be an object.");
        var result = new Dictionary<string, AiMcpConfiguration>(StringComparer.Ordinal);
        foreach (var server in servers.EnumerateObject())
        {
            if (server.Value.ValueKind != JsonValueKind.Object) throw new AiMcpConfigurationInvalid($"mcpServers.{server.Name} must be an object.");
            var enabled = true;
            string? root = null;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in server.Value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new AiMcpConfigurationInvalid($"Duplicate mcpServers.{server.Name}.{property.Name} setting.");
                switch (property.Name)
                {
                    case "enabled" when property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                        enabled = property.Value.GetBoolean();
                        break;
                    case "root" when property.Value.ValueKind == JsonValueKind.String:
                        root = property.Value.GetString()!;
                        AiProjectPaths.ValidateRelative(root);
                        break;
                    default:
                        throw new AiMcpConfigurationInvalid($"Invalid mcpServers.{server.Name}.{property.Name} setting.");
                }
            }
            if (!result.TryAdd(server.Name, new(enabled, root))) throw new AiMcpConfigurationInvalid($"Duplicate MCP server setting: {server.Name}");
        }
        return result;
    }

    internal static JsonObject ToJson(IReadOnlyDictionary<string, AiMcpConfiguration> servers)
    {
        var result = new JsonObject();
        foreach (var (id, settings) in servers)
        {
            var value = new JsonObject { ["enabled"] = settings.Enabled };
            if (settings.Root is not null) value["root"] = settings.Root;
            result[id] = value;
        }
        return result;
    }
}
