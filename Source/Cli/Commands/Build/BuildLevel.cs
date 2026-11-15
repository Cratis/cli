// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// How much of an application the packaged Cratis builder produces, the same three levels Studio offers in the cloud.
/// </summary>
public enum BuildLevel
{
    /// <summary>The Screenplay model is the application: the builder commits the model and writes no code.</summary>
    Screenplay = 0,

    /// <summary>The model is rendered into code deterministically, with no model calls.</summary>
    Code = 1,

    /// <summary>The model is rendered, then an agent completes what the renderer leaves for judgment.</summary>
    CodeWithAI = 2
}

/// <summary>
/// Names the levels and render targets of the packaged builder as they are written on the command line.
/// </summary>
public static class BuildLevels
{
    /// <summary>The Cratis render target: .NET on Arc and Chronicle with a React frontend.</summary>
    public const string CratisTarget = "cratis";

    /// <summary>The Axon render target: a Java application on Axon Framework.</summary>
    public const string AxonTarget = "axon";

    /// <summary>
    /// Gets the names a level is written as.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = ["screenplay", "code", "code-ai"];

    /// <summary>
    /// Reads a level.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="level">The level, when the name is one.</param>
    /// <returns><see langword="true"/> when the name is a level.</returns>
    public static bool TryParse(string? name, out BuildLevel level)
    {
        level = name?.ToLowerInvariant() switch
        {
            "screenplay" => BuildLevel.Screenplay,
            "code" => BuildLevel.Code,
            "code-ai" => BuildLevel.CodeWithAI,
            _ => (BuildLevel)(-1)
        };
        return Enum.IsDefined(level);
    }

    /// <summary>
    /// Gets whether a render target is one the packaged builder knows.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    public static bool IsKnownTarget(string target) =>
        string.Equals(target, CratisTarget, StringComparison.OrdinalIgnoreCase) || string.Equals(target, AxonTarget, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets what the builder calls itself in Studio.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="target">The render target.</param>
    /// <returns>The kit name Studio shows.</returns>
    public static string KitName(BuildLevel level, string target) => $"{target.ToLowerInvariant()}-{Names[(int)level]}";
}
