// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Normalizes and orders the version strings release notes are keyed by.
/// </summary>
public static class ReleaseNoteVersion
{
    /// <summary>
    /// Normalizes a version or tag: drops a leading <c language="csharp">v</c> and any build metadata.
    /// </summary>
    /// <param name="version">The version or tag.</param>
    /// <returns>The normalized version.</returns>
    public static string Normalize(string version)
    {
        var trimmed = version.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }

        var plus = trimmed.IndexOf('+');
        return plus >= 0 ? trimmed[..plus] : trimmed;
    }

    /// <summary>
    /// Compares two versions: numerically by their core, a prerelease below its release.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns>Less than zero when <paramref name="left"/> is older, zero when equal, greater than zero when newer.</returns>
    public static int Compare(string left, string right)
    {
        var (leftCore, leftPrerelease) = Split(Normalize(left));
        var (rightCore, rightPrerelease) = Split(Normalize(right));

        var core = leftCore.CompareTo(rightCore);
        if (core != 0)
        {
            return core;
        }

        if (leftPrerelease.Length == 0)
        {
            return rightPrerelease.Length == 0 ? 0 : 1;
        }

        return rightPrerelease.Length == 0 ? -1 : string.CompareOrdinal(leftPrerelease, rightPrerelease);
    }

    static (System.Version Core, string Prerelease) Split(string version)
    {
        var dash = version.IndexOf('-');
        var core = dash >= 0 ? version[..dash] : version;
        var prerelease = dash >= 0 ? version[(dash + 1)..] : string.Empty;
        return (System.Version.TryParse(core, out var parsed) ? parsed : new System.Version(0, 0), prerelease);
    }
}
