// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Reads the latest published version from the places a CLI installation can come from.
/// </summary>
public static class LatestVersion
{
    /// <summary>
    /// The GitHub repository the native downloads and the Homebrew formula are released from.
    /// </summary>
    public const string GitHubRepository = "Cratis/cli";

    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Resolves the place an installation updating through the given strategy should be compared against.
    /// </summary>
    /// <param name="strategy">The detected update strategy.</param>
    /// <returns>The <see cref="LatestVersionSource"/> to read from.</returns>
    /// <remarks>
    /// Only the dotnet tool installs from NuGet. Every native installation - Homebrew, the Linux tarball, or a
    /// binary put somewhere by hand - is built from a GitHub release, so that is the version those have to be
    /// told about. The Homebrew formula is written by the same workflow after the release exists, which means
    /// the release can never be behind the tap.
    /// </remarks>
    public static LatestVersionSource SourceFor(CliUpdateStrategy strategy) =>
        strategy == CliUpdateStrategy.DotNetTool
            ? LatestVersionSource.NuGet
            : LatestVersionSource.GitHubRelease;

    /// <summary>
    /// Reads the latest stable version of a package from NuGet.
    /// </summary>
    /// <param name="packageId">The NuGet package identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The latest stable version, or null when it could not be read.</returns>
    public static async Task<string?> FromNuGet(string packageId, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = _timeout };
        var url = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
        var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("versions", out var versions) ||
            versions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        // NuGet returns versions in ascending order; the last stable version is what we want.
        string? latest = null;

        foreach (var v in versions.EnumerateArray())
        {
            var versionString = v.GetString();
            if (versionString?.Contains('-') == false)
            {
                latest = versionString;
            }
        }

        return latest;
    }

    /// <summary>
    /// Reads the version of the latest GitHub release.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The latest released version, or null when it could not be read.</returns>
    public static async Task<string?> FromGitHubRelease(CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = _timeout };

        // GitHub rejects requests without a user agent, and serves the release metadata under its own media type.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Cratis.Cli");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var url = $"https://api.github.com/repos/{GitHubRepository}/releases/latest";
        var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        return document.RootElement.TryGetProperty("tag_name", out var tag)
            ? NormalizeTag(tag.GetString())
            : null;
    }

    /// <summary>
    /// Turns a release tag into a comparable version.
    /// </summary>
    /// <param name="tagName">The tag name, which the release workflow writes as <c>v{version}</c>.</param>
    /// <returns>The version without the tag prefix, or null when there was nothing to read.</returns>
    internal static string? NormalizeTag(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        var trimmed = tagName.Trim();
        return trimmed.StartsWith('v') || trimmed.StartsWith('V')
            ? trimmed[1..]
            : trimmed;
    }
}
