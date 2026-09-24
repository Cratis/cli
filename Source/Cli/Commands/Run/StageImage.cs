// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Reads what Docker already knows about the Stage image on this computer - which tags have been pulled - and
/// builds the arguments to pull a newer one.
/// </summary>
public static class StageImage
{
    /// <summary>
    /// Builds the argument list for listing the tags of the Stage image Docker already has locally.
    /// </summary>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildListTagsArguments() =>
        ["images", "--format", "{{.Tag}}", StageContainer.Image];

    /// <summary>
    /// Builds the argument list for pulling a specific tag of the Stage image.
    /// </summary>
    /// <param name="tag">The tag to pull.</param>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildPullArguments(string tag) => ["pull", $"{StageContainer.Image}:{tag}"];

    /// <summary>
    /// Reads the newest version tag from what <see cref="BuildListTagsArguments"/> answered.
    /// </summary>
    /// <param name="output">What Docker wrote.</param>
    /// <returns>The newest tag that parses as a version, or null when none of them do - including when the
    /// image has never been pulled at all, which prints nothing rather than a tag.</returns>
    /// <remarks>
    /// A tag such as <c language="csharp">latest</c> or a locally built <c language="csharp">4.0.0-15-g046e3f9-dirty</c> does not parse as a
    /// version and is skipped rather than compared - the same rule <see cref="UpdateChecker.IsNewer"/> already
    /// applies to what it reads from NuGet, so a moving or dev tag never gets treated as "the version".
    /// </remarks>
    public static string? LatestVersionFrom(string output)
    {
        string? latestTag = null;
        System.Version? latest = null;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = line.Trim();
            if (!System.Version.TryParse(tag, out var version))
            {
                continue;
            }

            if (latest is null || version > latest)
            {
                latest = version;
                latestTag = tag;
            }
        }

        return latestTag;
    }
}
