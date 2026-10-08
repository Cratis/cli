// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
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

    byte[]? _original;

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
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var reader = new StreamReader(stream);
            var manifest = JsonSerializer.Deserialize<DirectMcpManifest>(reader.ReadToEnd()) is { Servers: not null } read
                ? read
                : throw new AiMcpConfigurationInvalid($"{RelativePath} is not a valid Direct MCP manifest.");
            manifest._original = bytes;
            return manifest;
        }
        catch (JsonException)
        {
            throw new AiMcpConfigurationInvalid($"{RelativePath} is not a valid Direct MCP manifest.");
        }
    }

    /// <summary>Excludes other applying commands in this scope; a busy scope is refused instead of waiting.</summary>
    /// <param name="root">The physical scope root.</param>
    /// <param name="home">The user's home directory, where scope locks are reused without leaving project files.</param>
    /// <returns>The lease, held until every client file and manifest write is complete.</returns>
    /// <exception cref="AiMcpConfigurationInvalid">When another command is applying in this scope.</exception>
    internal static IDisposable AcquireLock(string root, string home)
    {
        var scope = AiPhysicalRoot.Resolve(root);
        if (OperatingSystem.IsWindows()) scope = scope.ToUpperInvariant();
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
        var path = AiProjectPaths.Within(home, $".cratis/direct-mcp-locks/{key}.lock");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.OpenOrCreate, Access = FileAccess.ReadWrite, Share = FileShare.None };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            return new FileStream(path, options);
        }
        catch (IOException)
        {
            throw new AiMcpConfigurationInvalid("Another Direct MCP registration command is applying in this scope; run the command again.");
        }
    }

    /// <summary>Rejects a plan whose ownership record changed since it was read.</summary>
    /// <param name="root">The physical scope root.</param>
    /// <exception cref="AiMcpConfigurationInvalid">When the manifest changed after planning.</exception>
    internal void ConfirmUnchanged(string root)
    {
        var path = AiProjectPaths.Within(root, RelativePath);
        var current = File.Exists(path) ? File.ReadAllBytes(path) : null;
        if ((_original is null) != (current is null) || _original?.SequenceEqual(current) == false)
        {
            throw new AiMcpConfigurationInvalid($"{RelativePath} changed during MCP registration; run the command again.");
        }
    }

    /// <summary>Writes the manifest, or deletes it when nothing is owned any more.</summary>
    /// <param name="root">The physical scope root.</param>
    /// <param name="operations">The file operations, which may be a dry run.</param>
    /// <param name="previous">The exact manifest this write replaces.</param>
    internal void Write(string root, AiFileOperations operations, DirectMcpManifest previous)
    {
        if (operations.DryRun) return;
        previous.ConfirmUnchanged(root);
        var path = AiProjectPaths.Within(root, RelativePath);
        if (Servers.Count == 0)
        {
            if (File.Exists(path)) operations.DeleteFile(path);
            _original = null;
            return;
        }
        var content = JsonSerializer.Serialize(this, _options) + "\n";
        operations.CreateDirectoryFor(path);
        operations.WriteAllTextAtomically(path, content, () => previous.ConfirmUnchanged(root));
        _original = Encoding.UTF8.GetBytes(content);
    }
}
