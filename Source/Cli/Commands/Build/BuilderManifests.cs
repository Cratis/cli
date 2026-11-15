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
    /// Describes a builder: from its kit, or, for a generator without one, every slice type's backend. The packaged builder
    /// names itself by target and level (such as cratis-code-ai or axon-ai), so Studio shows which of its four
    /// implementation modes the builder runs; at every level but screenplay the Cratis target also builds user interfaces,
    /// while the Axon target builds backends only.
    /// </summary>
    /// <param name="kit">The kit, or <see langword="null"/>.</param>
    /// <param name="generator">The name shown for a builder without a kit.</param>
    /// <param name="level">The level of the packaged Cratis builder, or <see langword="null"/> for an agent or generator of your own.</param>
    /// <param name="target">The render target of the packaged builder.</param>
    /// <returns>The manifest.</returns>
    public static JsonObject For(BuildKit? kit, string generator, BuildLevel? level = null, string? target = null)
    {
        JsonArray parts = ["backend"];
        if (kit?.BuildsUserInterfaces == true || (level is { } built && built != BuildLevel.Screenplay && target != BuildLevels.AxonTarget)) parts.Add("ui");
        var contract = kit is null ? ContractVersions : [.. ContractVersions.Intersect(kit.ContractVersions, StringComparer.Ordinal)];
        return new JsonObject
        {
            ["stacks"] = new JsonArray([.. new[] { kit?.Name, StackOf(level, target) }.OfType<string>().Select(name => (JsonNode)name)]),
            ["parts"] = parts,
            ["sliceTypes"] = new JsonArray([.. (kit?.SliceTypes ?? []).Select(type => (JsonNode)type)]),
            ["screenplayVersions"] = new JsonArray("1"),
            ["contractVersions"] = new JsonArray([.. contract.Select(version => (JsonNode)version)]),
            ["kit"] = kit?.Name ?? (level is { } chosen ? BuildLevels.KitName(chosen, target ?? BuildLevels.CratisTarget) : generator),
            ["gitHandling"] = new JsonArray(BuildGitInstructions.StudioManaged, BuildGitInstructions.BuilderManaged)
        };
    }

    static string? StackOf(BuildLevel? level, string? target)
    {
        if (level is null) return null;

        return string.Equals(target, BuildLevels.AxonTarget, StringComparison.Ordinal) ? "java" : "dotnet";
    }
}
