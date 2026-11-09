// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Composes what a builder tells Studio it can build when it connects.
/// </summary>
public static class BuilderManifests
{
    /// <summary>The build contract versions this host reads.</summary>
    public static readonly IReadOnlyList<string> ContractVersions = ["1"];

    /// <summary>
    /// Describes a builder: from its kit, or, for a generator without one, every slice type's backend.
    /// </summary>
    /// <param name="kit">The kit, or <see langword="null"/>.</param>
    /// <param name="generator">The name shown for a builder without a kit.</param>
    /// <returns>The manifest.</returns>
    public static JsonObject For(BuildKit? kit, string generator)
    {
        JsonArray parts = ["backend"];
        if (kit?.BuildsUserInterfaces == true) parts.Add("ui");
        var contract = kit is null ? ContractVersions : [.. ContractVersions.Intersect(kit.ContractVersions, StringComparer.Ordinal)];
        return new JsonObject
        {
            ["stacks"] = new JsonArray([.. new[] { kit?.Name }.OfType<string>().Select(name => (JsonNode)name)]),
            ["parts"] = parts,
            ["sliceTypes"] = new JsonArray([.. (kit?.SliceTypes ?? []).Select(type => (JsonNode)type)]),
            ["screenplayVersions"] = new JsonArray("1"),
            ["contractVersions"] = new JsonArray([.. contract.Select(version => (JsonNode)version)]),
            ["kit"] = kit?.Name ?? generator,
            ["gitHandling"] = new JsonArray(BuildGitInstructions.StudioManaged, BuildGitInstructions.BuilderManaged)
        };
    }
}
