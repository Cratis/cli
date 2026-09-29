// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Plans member-level changes to native MCP configuration files beneath one root directory, owning only the members
/// it installed. A member that was never installed, or that changed since, is reported as a conflict and never
/// replaced; every other member and every other part of the file is left untouched.
/// </summary>
/// <param name="root">The physical directory the configuration paths are relative to.</param>
/// <param name="removedOwnedIsAbsent">
/// Whether an owned member that is no longer in its file is treated as absent, so installing adds it again and removing
/// forgets it. Otherwise its removal is drift like any other change to an owned member, and blocks the plan.
/// </param>
internal sealed class AiMcpMembers(string root, bool removedOwnedIsAbsent = false)
{
    readonly Dictionary<string, IAiMcpDocument> _documents = new(StringComparer.Ordinal);

    /// <summary>Gets the members owned after the plan is applied.</summary>
    internal List<AiManagedMcpServer> Installed { get; } = [];

    /// <summary>Gets members that block the plan.</summary>
    internal List<string> Conflicts { get; } = [];

    /// <summary>Gets a description of each change.</summary>
    internal List<string> Actions { get; } = [];

    /// <summary>Gets the exact member values each change replaces and writes.</summary>
    internal List<AiMcpChange> Changes { get; } = [];

    /// <summary>Plans installing the desired members and removing previously owned members that are no longer desired.</summary>
    /// <param name="desired">The members to install.</param>
    /// <param name="previous">The members recorded as owned.</param>
    /// <param name="validateOwned">Rejects an ownership record that does not describe an allowed member.</param>
    internal void Prepare(IReadOnlyList<AiManagedMcpServer> desired, IReadOnlyList<AiManagedMcpServer> previous, Action<AiManagedMcpServer> validateOwned)
    {
        foreach (var entry in previous)
        {
            validateOwned(entry);
            var document = Document(entry.Path);
            var current = document.Get(entry.Collection, entry.Id);
            if (removedOwnedIsAbsent && !document.Contains(entry.Collection, entry.Id)) continue;
            if (!JsonNode.DeepEquals(current, entry.Installed))
            {
                Conflicts.Add($"{entry.Path}:{entry.Collection}.{entry.Id} (modified owned MCP entry)");
                continue;
            }
            if (desired.Any(candidate => SameMember(candidate, entry))) continue;
            document.Set(entry.Collection, entry.Id, entry.Preimage);
            Changes.Add(new(entry.Harness, entry.Path, entry.Collection, entry.Id, current?.DeepClone(), entry.Preimage));
            Actions.Add($"Removed MCP {entry.Harness}/{entry.Id} from {entry.Path}");
        }
        foreach (var entry in desired)
        {
            var document = Document(entry.Path);
            var owned = previous.FirstOrDefault(candidate => SameMember(candidate, entry));
            var current = document.Get(entry.Collection, entry.Id);
            if (owned is null && document.Contains(entry.Collection, entry.Id))
            {
                Conflicts.Add($"{entry.Path}:{entry.Collection}.{entry.Id} (user-owned MCP entry)");
                continue;
            }
            if (owned is not null && (!removedOwnedIsAbsent || document.Contains(entry.Collection, entry.Id)) && !JsonNode.DeepEquals(current, owned.Installed)) continue;
            if (!JsonNode.DeepEquals(current, entry.Installed))
            {
                document.Set(entry.Collection, entry.Id, entry.Installed);
                Changes.Add(new(entry.Harness, entry.Path, entry.Collection, entry.Id, current?.DeepClone(), entry.Installed));
                Actions.Add($"Configured MCP {entry.Harness}/{entry.Id} in {entry.Path}");
            }
            Installed.Add(entry);
        }
    }

    /// <summary>Reads a member's current value.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="collection">The server collection.</param>
    /// <param name="id">The member name.</param>
    /// <returns>The value, or null when absent.</returns>
    internal JsonNode? Get(string path, string collection, string id) => Document(path).Get(collection, id);

    /// <summary>Gets whether a member exists.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="collection">The server collection.</param>
    /// <param name="id">The member name.</param>
    /// <returns>True when the member exists, whatever its value.</returns>
    internal bool Contains(string path, string collection, string id) => Document(path).Contains(collection, id);

    /// <summary>Writes every changed document; a document changed by someone else since it was read is refused.</summary>
    /// <param name="operations">The file operations, which may be a dry run.</param>
    /// <exception cref="AiMcpConfigurationInvalid">When the plan has conflicts.</exception>
    internal void Apply(AiFileOperations operations)
    {
        if (Conflicts.Count > 0) throw new AiMcpConfigurationInvalid("Cannot apply an MCP plan with conflicts.");
        foreach (var document in _documents.Values) document.Apply(operations);
    }

    static bool SameMember(AiManagedMcpServer left, AiManagedMcpServer right) =>
        left.Path == right.Path && left.Collection == right.Collection && left.Id == right.Id;

    IAiMcpDocument Document(string path)
    {
        if (!_documents.TryGetValue(path, out var document))
        {
            document = path.EndsWith(".toml", StringComparison.Ordinal)
                ? new AiMcpTomlDocument(root, path)
                : new AiMcpDocument(root, path);
            _documents.Add(path, document);
        }
        return document;
    }
}

/// <summary>One planned member change.</summary>
/// <param name="Harness">The native harness adapter.</param>
/// <param name="Path">The configuration path relative to the root.</param>
/// <param name="Collection">The server collection.</param>
/// <param name="Id">The member name.</param>
/// <param name="Before">The value being replaced; null when absent.</param>
/// <param name="After">The value being written; null removes the member.</param>
internal sealed record AiMcpChange(string Harness, string Path, string Collection, string Id, JsonNode? Before, JsonNode? After);
