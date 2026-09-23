// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

internal static class AiMcpHarnesses
{
    internal static AiManagedMcpServer? Render(string project, string harness, AiMcpDescriptor descriptor)
    {
        // Only Screenplay currently defines the project-root arguments understood by the embedded CLI.
        if (descriptor.Id != "screenplay") return null;
        var args = new List<string>(descriptor.Args);
        switch (harness)
        {
            case "claude":
                // Read this in the child; Claude does not expand its injected environment in project args.
                args.AddRange(["--project-root-env", "CLAUDE_PROJECT_DIR"]);
                return Entry(harness, ".mcp.json", "mcpServers", descriptor, args);
            case "copilot":
                args.AddRange(["--project-root", "${workspaceFolder}"]);
                return Entry(harness, ".vscode/mcp.json", "servers", descriptor, args);
            case "cursor":
                args.AddRange(["--project-root", "${workspaceFolder}"]);
                return Entry(harness, ".cursor/mcp.json", "mcpServers", descriptor, args);
            case "codex":
                args.AddRange(["--project-root", "."]);
                return new(harness, ".codex/config.toml", "mcp_servers", descriptor.Id, new JsonObject
                {
                    ["command"] = descriptor.Command,
                    ["args"] = new JsonArray([.. args.Select(value => (JsonNode?)JsonValue.Create(value))])
                });
            case "opencode":
                // OpenCode's local transport sets cwd to InstanceState.directory. Require this exact
                // project to contain ai.json rather than guessing an ancestor or another checkout.
                args.AddRange(["--project-root", "."]);
                var path = OpenCodePath(project);
                return new(harness, path, "mcp", descriptor.Id, new JsonObject
                {
                    ["type"] = "local",
                    ["command"] = new JsonArray([.. new[] { descriptor.Command }.Concat(args).Select(value => (JsonNode?)JsonValue.Create(value))]),
                    ["enabled"] = true
                });
            default:
                return null;
        }
    }

    internal static string Unsupported(string harness, string id) => harness switch
    {
        "pi" => $"{harness}/{id}: corpus does not include the native cratis-mcp Pi extension; update the corpus before using MCP.",
        _ => $"{harness}/{id}: unsupported MCP adapter."
    };

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
        if (!valid || entry.Id != "screenplay" || entry.Preimage is not null) throw new AiMcpConfigurationInvalid("Invalid managed MCP entry in .cratis/ai.manifest.json.");
    }

    static AiManagedMcpServer Entry(string harness, string path, string collection, AiMcpDescriptor descriptor, List<string> args) =>
        new(harness, path, collection, descriptor.Id, new JsonObject
        {
            ["type"] = "stdio",
            ["command"] = descriptor.Command,
            ["args"] = new JsonArray([.. args.Select(value => (JsonNode?)JsonValue.Create(value))])
        });

    static string OpenCodePath(string project)
    {
        var json = AiProjectPaths.Within(project, "opencode.json");
        var jsonc = AiProjectPaths.Within(project, "opencode.jsonc");
        if (File.Exists(json) && File.Exists(jsonc)) throw new AiMcpConfigurationInvalid("Both opencode.json and opencode.jsonc exist; select one before registering MCP.");
        return File.Exists(jsonc) ? "opencode.jsonc" : "opencode.json";
    }
}
