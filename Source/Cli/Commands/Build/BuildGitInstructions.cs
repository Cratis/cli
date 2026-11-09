// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Who does the repository work for one build, and how: fixed by Studio when the slice is claimed.
/// </summary>
/// <param name="Handling">Either <see cref="StudioManaged"/> or <see cref="BuilderManaged"/>.</param>
/// <param name="Branch">The branch Studio named, in a Studio-managed build.</param>
/// <param name="CommitMessage">The commit message Studio wrote, in a Studio-managed build.</param>
public sealed record BuildGitInstructions(string Handling, string? Branch, string? CommitMessage)
{
    /// <summary>Studio names the branch and commit message and opens the pull request; the builder pushes the commit.</summary>
    public const string StudioManaged = "studioManaged";

    /// <summary>The builder chooses its branch and commit message.</summary>
    public const string BuilderManaged = "builderManaged";

    /// <summary>
    /// Gets the instructions for a build with no Studio to ask, such as an exported order built offline.
    /// </summary>
    public static readonly BuildGitInstructions Offline = new(BuilderManaged, null, null);

    /// <summary>
    /// Gets a value indicating whether Studio manages the repository work.
    /// </summary>
    public bool IsStudioManaged => Handling == StudioManaged;

    /// <summary>
    /// Reads the instructions Studio handed over with a claimed slice.
    /// </summary>
    /// <param name="node">The <c language="csharp">git</c> member of Studio's answer.</param>
    /// <returns>The instructions.</returns>
    public static BuildGitInstructions From(JsonNode? node) => new(
        Text(node?["gitHandling"]) == StudioManaged ? StudioManaged : BuilderManaged,
        Text(node?["branch"]),
        Text(node?["commitMessage"]));

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 0 ? text : null;
}
