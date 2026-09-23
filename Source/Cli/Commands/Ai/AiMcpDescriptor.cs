// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Trusted corpus metadata; installation renders it but never executes its command.
/// </summary>
/// <param name="Id">Stable server identity.</param>
/// <param name="Profiles">Profiles selecting this server.</param>
/// <param name="Command">Trusted executable name.</param>
/// <param name="Args">Trusted argument list.</param>
/// <param name="DefaultRoot">Portable default model directory.</param>
internal sealed record AiMcpDescriptor(string Id, IReadOnlyList<string> Profiles, string Command, IReadOnlyList<string> Args, string DefaultRoot)
{
    internal const string RelativePath = ".cratis/ai/mcp-servers.json";
    internal const string ScreenplayRoot = ".cratis/screenplay";

    internal static IReadOnlyList<AiMcpDescriptor> Read(string project)
    {
        var path = AiProjectPaths.Within(project, RelativePath);
        if (!File.Exists(path)) return [];
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetString() != "1.0") throw new AiMcpConfigurationInvalid("Unsupported MCP descriptor schemaVersion.");
        var result = new List<AiMcpDescriptor>();
        foreach (var server in root.GetProperty("servers").EnumerateArray())
        {
            var id = server.GetProperty("id").GetString()!;
            if (string.IsNullOrWhiteSpace(id) || result.Exists(existing => existing.Id == id)) throw new AiMcpConfigurationInvalid($"Invalid or duplicate MCP server id: '{id}'.");
            if (server.GetProperty("transport").GetString() != "stdio") throw new AiMcpConfigurationInvalid($"MCP server '{id}' has an unsupported transport.");
            var command = server.GetProperty("command").GetString()!;
            var args = server.GetProperty("args").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var profiles = server.GetProperty("profiles").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var defaultRoot = server.GetProperty("defaultRoot").GetString()!;
            AiProjectPaths.ValidateRelative(defaultRoot);
            if (string.IsNullOrWhiteSpace(command) || args.Any(string.IsNullOrWhiteSpace) || profiles.Length == 0) throw new AiMcpConfigurationInvalid($"Incomplete MCP server descriptor: {id}");
            result.Add(new(id, profiles, command, args, defaultRoot));
        }
        return result;
    }

    internal string Root(AiConfiguration configuration)
    {
        var root = configuration.McpServers?.GetValueOrDefault(Id)?.Root ?? DefaultRoot;
        AiProjectPaths.ValidateRelative(root);
        return root;
    }
}
