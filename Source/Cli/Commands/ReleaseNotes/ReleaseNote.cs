// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Represents the release notes of one released version of the CLI.
/// </summary>
/// <param name="Version">The released version, without a leading <c language="csharp">v</c>.</param>
/// <param name="Notes">The notes as markdown, or null when this build does not include them.</param>
public record ReleaseNote(string Version, string? Notes)
{
    /// <summary>
    /// Gets the page of the release on GitHub, which always has the notes.
    /// </summary>
    public string Url => $"https://github.com/{LatestVersion.GitHubRepository}/releases/tag/v{Version}";
}
