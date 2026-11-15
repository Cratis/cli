// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// How much of an application the packaged Cratis builder produces and how: the four implementation modes Studio offers in the
/// cloud (Screenplay only, Static code, Static code refined by AI and AI only).
/// </summary>
public enum BuildLevel
{
    /// <summary>Studio's Screenplay only: the model is the application; the builder commits the model and writes no code.</summary>
    Screenplay = 0,

    /// <summary>Studio's Static code: the model is rendered into code deterministically, with no model calls.</summary>
    Code = 1,

    /// <summary>
    /// Studio's Static code refined by AI: the model is rendered deterministically, then an agent implements the behavior the model
    /// describes on top of the rendered structure.
    /// </summary>
    CodeWithAI = 2,

    /// <summary>Studio's AI only: the model is committed and an agent implements the slices from it, with no render.</summary>
    AI = 3
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
    public static IReadOnlyList<string> Names { get; } = ["screenplay", "code", "code-ai", "ai"];

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
            "ai" => BuildLevel.AI,
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

    /// <summary>
    /// Gets whether the renderer runs at a level.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns><see langword="true"/> when the model is rendered into code.</returns>
    public static bool Renders(BuildLevel level) => level is BuildLevel.Code or BuildLevel.CodeWithAI;

    /// <summary>
    /// Gets whether an agent writes code at a level, so one has to be named.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns><see langword="true"/> for the code-ai and ai levels.</returns>
    public static bool NeedsAgent(BuildLevel level) => level is BuildLevel.CodeWithAI or BuildLevel.AI;

    /// <summary>
    /// Gets the Studio implementation mode a level corresponds to, as Studio names it.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>The mode's name.</returns>
    public static string StudioMode(BuildLevel level) => level switch
    {
        BuildLevel.Screenplay => "Screenplay only",
        BuildLevel.Code => "Static code",
        BuildLevel.CodeWithAI => "Static code refined by AI",
        _ => "AI only"
    };
}
