// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Represents where the latest available version of the CLI should be read from.
/// </summary>
/// <remarks>
/// An installation has to be compared against the place it actually updates from. A tool installed with
/// <c>dotnet tool install</c> updates from NuGet, while the native downloads - Homebrew included - update from
/// the GitHub releases. Those are published by separate jobs and do not become visible at the same moment.
/// </remarks>
public enum LatestVersionSource
{
    /// <summary>
    /// Read the latest version from the NuGet package feed.
    /// </summary>
    NuGet,

    /// <summary>
    /// Read the latest version from the GitHub releases.
    /// </summary>
    GitHubRelease
}
