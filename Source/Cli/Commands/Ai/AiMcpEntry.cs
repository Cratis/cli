// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// One leniently parsed descriptor entry: either a usable descriptor or the problem that makes it skippable.
/// </summary>
/// <param name="Id">The entry id, or an empty string when absent.</param>
/// <param name="Profiles">The profiles that would select the entry.</param>
/// <param name="Descriptor">The descriptor when the entry is complete and supported.</param>
/// <param name="Problem">Why the entry is skipped, or <see langword="null"/>.</param>
internal sealed record AiMcpEntry(string Id, IReadOnlyList<string> Profiles, AiMcpDescriptor? Descriptor, string? Problem)
{
    const string Incomplete = "incomplete descriptor";

    internal static AiMcpEntry Parse(JsonElement server)
    {
        var id = Text(server, "id") ?? string.Empty;
        var profiles = Strings(server, "profiles") ?? [];
        var problem = Validate(server, id, out var command, out var args, out var env);
        if (problem is not null) return new(id, profiles, null, problem);
        if (id == "screenplay" && (command != "cratis" || !args!.SequenceEqual(["screenplay", "mcp"], StringComparer.Ordinal)))
        {
            throw new AiMcpConfigurationInvalid($"MCP server 'screenplay' must launch 'cratis screenplay mcp'; refusing to register '{command}'.");
        }
        return new(id, profiles, new(id, profiles, command!, args!, env), null);
    }

    static string? Validate(JsonElement server, string id, out string? command, out string[]? args, out Dictionary<string, string>? env)
    {
        command = Text(server, "command");
        args = Strings(server, "args");
        env = null;
        if (id.Length == 0 || (Strings(server, "profiles") ?? []).Length == 0 || string.IsNullOrWhiteSpace(command) || args?.Any(string.IsNullOrWhiteSpace) != false) return Incomplete;
        if (!AiMcpDescriptor.IsValidId(id)) return "invalid id";
        if (Text(server, "transport") != "stdio") return "unsupported transport";
        return ReadEnvironment(server, out env);
    }

    static string? ReadEnvironment(JsonElement server, out Dictionary<string, string>? result)
    {
        result = null;
        if (!server.TryGetProperty("env", out var env)) return null;
        if (env.ValueKind != JsonValueKind.Object) return "env must be an object of strings";
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in env.EnumerateObject())
        {
            if (variable.Value.ValueKind != JsonValueKind.String) return $"env value '{variable.Name}' must be a string";
            variables[variable.Name] = variable.Value.GetString()!;
        }
        result = variables.Count == 0 ? null : variables;
        return null;
    }

    static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    static string[]? Strings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array) return null;
        return [.. value.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()! : string.Empty)];
    }
}
