// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Corpus metadata validated before installation renders a native MCP registration.
/// </summary>
/// <param name="Id">Stable server identity.</param>
/// <param name="Profiles">Profiles selecting this server.</param>
/// <param name="Command">Validated executable name.</param>
/// <param name="Args">Validated argument list.</param>
/// <param name="Env">Optional environment variables for the server process, or <see langword="null"/>.</param>
internal sealed record AiMcpDescriptor(string Id, IReadOnlyList<string> Profiles, string Command, IReadOnlyList<string> Args, IReadOnlyDictionary<string, string>? Env = null)
{
    internal const string RelativePath = ".cratis/ai/mcp-servers.json";

    internal static AiMcpCatalog Read(string project)
    {
        var path = AiProjectPaths.Within(project, RelativePath);
        if (!File.Exists(path)) return AiMcpCatalog.Empty;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var version = root.TryGetProperty("schemaVersion", out var versionElement) && versionElement.ValueKind == JsonValueKind.String ? versionElement.GetString()! : string.Empty;
        if (!version.StartsWith("1.", StringComparison.Ordinal)) return new([], [], $"MCP descriptors skipped: unsupported schemaVersion '{version}'.");
        var entries = root.TryGetProperty("servers", out var servers) && servers.ValueKind == JsonValueKind.Array ? servers.EnumerateArray().Select(AiMcpEntry.Parse).ToList() : [];
        var duplicates = entries.GroupBy(entry => entry.Id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var result = new List<AiMcpDescriptor>();
        var skipped = new List<AiMcpSkipped>();
        foreach (var entry in entries)
        {
            var reason = duplicates.Contains(entry.Id) ? "duplicate id" : entry.Problem;
            if (reason is not null) skipped.Add(new(entry.Id, entry.Profiles, reason));
            else result.Add(entry.Descriptor!);
        }
        return new(result, skipped, null);
    }

    internal static bool IsValidId(string id) => id.Length > 0 && id.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    /// <summary>
    /// Gets the model directory the project configured explicitly, or <see langword="null"/> when the server locates the model itself.
    /// </summary>
    /// <param name="configuration">The project configuration.</param>
    /// <returns>The validated project-relative directory, or <see langword="null"/>.</returns>
    internal string? Root(AiConfiguration configuration)
    {
        var root = configuration.McpServers?.GetValueOrDefault(Id)?.Root;
        if (root is not null) AiProjectPaths.ValidateRelative(root);
        return root;
    }
}
