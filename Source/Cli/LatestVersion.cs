// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

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

        return HighestStable(versions.EnumerateArray().Select(version => version.GetString()));
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
        ThrowIfRateLimited(response, GitHubRepository);

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
    /// Picks the highest stable version from a list of version strings.
    /// </summary>
    /// <param name="versions">The version strings, in any order.</param>
    /// <returns>The highest stable version that could be parsed, or null when there is none.</returns>
    /// <remarks>
    /// The NuGet index happens to list versions in ascending order, but nothing promises it, so the position of a
    /// version says nothing about it. Prereleases and anything that does not parse as a version are skipped.
    /// </remarks>
    internal static string? HighestStable(IEnumerable<string?> versions)
    {
        string? highest = null;
        Version? highestVersion = null;

        foreach (var candidate in versions)
        {
            if (candidate?.Contains('-') == false &&
                Version.TryParse(candidate, out var version) &&
                (highestVersion is null || version > highestVersion))
            {
                highest = candidate;
                highestVersion = version;
            }
        }

        return highest;
    }

    /// <summary>
    /// Throws <see cref="SourceRateLimited"/> when GitHub refused a request because its rate limit is spent.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <param name="source">The source named in the exception.</param>
    /// <exception cref="SourceRateLimited">Thrown when the rate limit is spent.</exception>
    internal static void ThrowIfRateLimited(HttpResponseMessage response, string source)
    {
        var remaining = response.Headers.TryGetValues("X-RateLimit-Remaining", out var values) ? values.FirstOrDefault() : null;
        if (IsRateLimited(response.StatusCode, remaining))
        {
            throw new SourceRateLimited(source);
        }
    }

    /// <summary>
    /// Determines whether a GitHub response means the rate limit is spent.
    /// </summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="rateLimitRemaining">The value of the X-RateLimit-Remaining header, when present.</param>
    /// <returns>True when the request was refused because of the rate limit.</returns>
    /// <remarks>GitHub refuses a request over the limit with 429, or with 403 and no requests remaining.</remarks>
    internal static bool IsRateLimited(HttpStatusCode statusCode, string? rateLimitRemaining) =>
        statusCode is HttpStatusCode.TooManyRequests ||
        (statusCode is HttpStatusCode.Forbidden && rateLimitRemaining == "0");

    /// <summary>
    /// Turns a release tag into a comparable version.
    /// </summary>
    /// <param name="tagName">The tag name, which the release workflow writes as <c language="csharp">v{version}</c>.</param>
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
