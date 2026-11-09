// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The Screenplay session guidance, unpacked and verified in a cache outside every repository. It teaches an agent to read
/// a Screenplay model and build a slice from an order, so the project being built needs nothing installed.
/// </summary>
/// <param name="Root">The folder the bundle is unpacked in.</param>
/// <param name="Version">The release of the guidance.</param>
/// <param name="SystemPrompt">The text Claude Code appends to its system prompt, or <see langword="null"/>.</param>
/// <param name="Instructions">The instruction file for agents that open files next to it, or <see langword="null"/>.</param>
/// <param name="Context">The single self-contained document, or <see langword="null"/>.</param>
public sealed record ScreenplaySessionGuidance(string Root, string Version, string? SystemPrompt, string? Instructions, string? Context)
{
    /// <summary>The bundle layout this host reads.</summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// Gets a value indicating whether the bundle's root is a Claude Code plugin.
    /// </summary>
    public bool IsClaudePlugin => File.Exists(Path.Combine(Root, ".claude-plugin", "plugin.json"));

    /// <summary>
    /// Reads an unpacked bundle.
    /// </summary>
    /// <param name="root">The folder it is unpacked in.</param>
    /// <returns>The guidance.</returns>
    /// <exception cref="InvalidDataException">The folder holds no bundle this host reads.</exception>
    public static ScreenplaySessionGuidance Load(string root)
    {
        var manifestPath = Path.Combine(root, "bundle.json");
        if (!File.Exists(manifestPath)) throw new InvalidDataException("The Screenplay session bundle has no bundle.json.");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject ?? throw new InvalidDataException("The bundle's bundle.json is not an object.");
        var schema = manifest["schemaVersion"] is JsonValue value && value.TryGetValue<int>(out var number) ? number : 0;
        if (schema != SchemaVersion) throw new InvalidDataException($"The Screenplay session bundle has layout {schema}; this CLI reads layout {SchemaVersion}. Update the CLI.");

        var forms = manifest["forms"];
        return new(
            root,
            manifest["version"]?.GetValue<string>() ?? "unknown",
            Existing(root, forms?["claude-code"]?["systemPrompt"]),
            Existing(root, forms?["agents-md"]?["instructions"]),
            Existing(root, forms?["context"]?["document"]));
    }

    static string? Existing(string root, JsonNode? relative)
    {
        if (relative is not JsonValue value || !value.TryGetValue<string>(out var path)) return null;
        var full = Path.GetFullPath(Path.Combine(root, path));
        return full.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal) && File.Exists(full) ? full : null;
    }
}
