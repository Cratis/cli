// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// The client configuration members <c language="shell">cratis direct mcp install</c> wrote in one scope. Only these are
/// ever changed or removed; an entry with the same name that is not recorded here belongs to the user.
/// </summary>
/// <param name="Servers">The owned members.</param>
/// <param name="Pending">
/// The values an update is about to write over owned members, recorded before it writes them. Present only while an
/// update runs, or after one was interrupted; each value that reached its file then replaces the owned member's record.
/// </param>
public sealed record DirectMcpManifest(
    IReadOnlyList<AiManagedMcpServer> Servers,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<AiManagedMcpServer>? Pending = null)
{
    /// <summary>The manifest's path relative to the scope's root: the home directory or the project.</summary>
    internal const string RelativePath = ".cratis/direct-mcp.json";

    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    /// <summary>Reads the manifest of a scope root; absent means nothing is owned.</summary>
    /// <param name="root">The physical scope root.</param>
    /// <returns>The manifest.</returns>
    /// <exception cref="AiMcpConfigurationInvalid">When the manifest cannot be read.</exception>
    internal static DirectMcpManifest Read(string root)
    {
        var path = AiProjectPaths.Within(root, RelativePath);
        if (!File.Exists(path)) return new([]);
        try
        {
            return JsonSerializer.Deserialize<DirectMcpManifest>(File.ReadAllText(path)) is { Servers: not null } manifest
                ? manifest
                : throw new AiMcpConfigurationInvalid($"{RelativePath} is not a valid Direct MCP manifest.");
        }
        catch (JsonException)
        {
            throw new AiMcpConfigurationInvalid($"{RelativePath} is not a valid Direct MCP manifest.");
        }
    }

    /// <summary>Writes the manifest, or deletes it when nothing is owned any more.</summary>
    /// <param name="root">The physical scope root.</param>
    /// <param name="operations">The file operations, which may be a dry run.</param>
    internal void Write(string root, AiFileOperations operations)
    {
        var path = AiProjectPaths.Within(root, RelativePath);
        if (Servers.Count == 0)
        {
            if (File.Exists(path)) operations.DeleteFile(path);
            return;
        }
        operations.CreateDirectoryFor(path);
        operations.WriteAllTextAtomically(path, JsonSerializer.Serialize(this, _options) + "\n");
    }
}
