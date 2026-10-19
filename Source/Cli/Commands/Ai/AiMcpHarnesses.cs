// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

internal static class AiMcpHarnesses
{
    static readonly string[] _screenplayArgs = ["screenplay", "mcp"];
    static readonly string[] _openCodeArgs = ["cratis", "screenplay", "mcp"];

    internal static AiManagedMcpServer? Render(string project, string harness, AiMcpDescriptor descriptor)
    {
        var installed = Launch(harness, descriptor);
        if (installed is null) return null;

        // OpenCode's local transport sets cwd to InstanceState.directory. Require this exact
        // project to contain ai.json rather than guessing an ancestor or another checkout.
        return harness switch
        {
            "claude" => new(harness, ".mcp.json", "mcpServers", descriptor.Id, installed),
            "copilot" => new(harness, ".vscode/mcp.json", "servers", descriptor.Id, installed),
            "cursor" => new(harness, ".cursor/mcp.json", "mcpServers", descriptor.Id, installed),
            "codex" => new(harness, ".codex/config.toml", "mcp_servers", descriptor.Id, installed),
            _ => new(harness, OpenCodePath(project), "mcp", descriptor.Id, installed)
        };
    }

    /// <summary>
    /// Builds the native launch entry a harness stores for a descriptor.
    /// </summary>
    /// <param name="harness">The harness name.</param>
    /// <param name="descriptor">The server descriptor.</param>
    /// <returns>The entry, or <see langword="null"/> when the harness has no adapter.</returns>
    internal static JsonObject? Launch(string harness, AiMcpDescriptor descriptor)
    {
        if (harness is not ("claude" or "copilot" or "cursor" or "codex" or "opencode")) return null;
        var args = new List<string>(descriptor.Args);
        args.AddRange(RootArguments(harness, descriptor.Id));
        var environment = descriptor.Env is null ? null : new JsonObject(descriptor.Env.Select(variable => KeyValuePair.Create(variable.Key, (JsonNode?)JsonValue.Create(variable.Value))));
        JsonObject entry;
        if (harness == "opencode")
        {
            entry = new()
            {
                ["type"] = "local",
                ["command"] = Strings([descriptor.Command, .. args]),
                ["enabled"] = true
            };
            if (environment is not null) entry["environment"] = environment;
            return entry;
        }
        entry = harness == "codex" ? [] : new() { ["type"] = "stdio" };
        entry["command"] = descriptor.Command;
        entry["args"] = Strings(args);
        if (environment is not null) entry["env"] = environment;
        return entry;
    }

    internal static string Unsupported(string harness, string id) => harness switch
    {
        "pi" => $"{harness}/{id}: corpus does not include the native cratis-mcp Pi extension; update the corpus before using MCP.",
        _ => $"{harness}/{id}: unsupported MCP adapter."
    };

    internal static bool HasAllowedLaunch(AiManagedMcpServer entry, IReadOnlyList<AiMcpDescriptor> descriptors)
    {
        if (entry.Id != "screenplay")
        {
            var descriptor = descriptors.FirstOrDefault(candidate => candidate.Id == entry.Id);
            return descriptor is not null && JsonNode.DeepEquals(Launch(entry.Harness, descriptor), entry.Installed);
        }
        var suffix = RootArguments(entry.Harness, entry.Id);
        if (suffix.Length == 0) return false;
        var command = entry.Harness == "opencode" ? entry.Installed["command"] : entry.Installed["args"];
        var expected = entry.Harness == "opencode"
            ? _openCodeArgs.Concat(suffix)
            : _screenplayArgs.Concat(suffix);
        return (entry.Harness == "opencode" || JsonNode.DeepEquals(entry.Installed["command"], JsonValue.Create("cratis"))) &&
            JsonNode.DeepEquals(command, Strings(expected));
    }

    internal static void ValidateOwned(AiManagedMcpServer entry)
    {
        var valid = entry.Harness switch
        {
            "claude" => entry.Path == ".mcp.json" && entry.Collection == "mcpServers",
            "copilot" => entry.Path == ".vscode/mcp.json" && entry.Collection == "servers",
            "cursor" => entry.Path == ".cursor/mcp.json" && entry.Collection == "mcpServers",
            "codex" => entry.Path == ".codex/config.toml" && entry.Collection == "mcp_servers",
            "opencode" => (entry.Path == "opencode.json" || entry.Path == "opencode.jsonc") && entry.Collection == "mcp",
            _ => false
        };
        if (!valid || !AiMcpDescriptor.IsValidId(entry.Id) || entry.Preimage is not null) throw new AiMcpConfigurationInvalid("Invalid managed MCP entry in .cratis/ai.manifest.json.");
    }

    /// <summary>
    /// Only Screenplay defines the project-root arguments understood by the embedded CLI;
    /// every other server launches exactly as its descriptor says.
    /// </summary>
    /// <param name="harness">The harness name.</param>
    /// <param name="id">The server id.</param>
    /// <returns>The extra launch arguments.</returns>
    static string[] RootArguments(string harness, string id) => id != "screenplay" ? [] : harness switch
    {
        "claude" => ["--project-root-env", "CLAUDE_PROJECT_DIR"],
        "copilot" or "cursor" => ["--project-root", "${workspaceFolder}"],
        _ => ["--project-root", "."]
    };

    static JsonArray Strings(IEnumerable<string> values) => new([.. values.Select(value => (JsonNode?)JsonValue.Create(value))]);

    static string OpenCodePath(string project)
    {
        var json = AiProjectPaths.Within(project, "opencode.json");
        var jsonc = AiProjectPaths.Within(project, "opencode.jsonc");
        if (File.Exists(json) && File.Exists(jsonc)) throw new AiMcpConfigurationInvalid("Both opencode.json and opencode.jsonc exist; select one before registering MCP.");
        return File.Exists(jsonc) ? "opencode.jsonc" : "opencode.json";
    }
}
