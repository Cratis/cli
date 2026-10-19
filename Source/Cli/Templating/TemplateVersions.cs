// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Templates;

/// <summary>
/// Compares template package and CLI versions by their numeric release part; a prerelease suffix
/// or build metadata does not take part in the ordering.
/// </summary>
public static class TemplateVersions
{
    /// <summary>
    /// The version a CLI built without release versioning reports; it never blocks a template set.
    /// </summary>
    public const string DevelopmentCliVersion = "1.0.0";

    /// <summary>
    /// Parses the numeric release part of a version.
    /// </summary>
    /// <param name="version">The version text.</param>
    /// <returns>The parsed version, or null when it is not a version.</returns>
    public static Version? Parse(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var release = version.Split('-', '+')[0];
        return Version.TryParse(release, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Picks the highest version from a list, ignoring entries that are not versions.
    /// </summary>
    /// <param name="versions">The versions.</param>
    /// <returns>The highest version, or null when there is none.</returns>
    public static string? Latest(IEnumerable<string> versions) =>
        versions
            .Select(version => (Text: version, Parsed: Parse(version)))
            .Where(version => version.Parsed is not null)
            .OrderByDescending(version => version.Parsed)
            .Select(version => version.Text)
            .FirstOrDefault();

    /// <summary>
    /// Determines whether a CLI version satisfies a template set's minimum CLI version.
    /// </summary>
    /// <param name="cliVersion">The CLI version.</param>
    /// <param name="minimumCliVersion">The minimum CLI version the template set requires, if any.</param>
    /// <returns>True when the CLI can use the template set.</returns>
    public static bool IsSatisfiedBy(string cliVersion, string? minimumCliVersion)
    {
        var minimum = Parse(minimumCliVersion);
        var cli = Parse(cliVersion);
        if (minimum is null || cli is null || cliVersion == DevelopmentCliVersion)
        {
            return true;
        }

        return cli >= minimum;
    }
}
