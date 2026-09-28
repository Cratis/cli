// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Checks, without touching the repository, whether 'cratis ai update' would bring in a newer Cratis AI corpus.
/// </summary>
/// <remarks>
/// The corpus has no version number. 'cratis ai update' clones the default branch of Cratis/AI and records the
/// commit it installed from in .cratis/ai.manifest.json. This check asks GitHub to compare that commit with the
/// default branch - one short request, cached alongside the CLI's own update check - and reports an update only
/// when the installed commit is an ancestor the branch has moved past. A commit GitHub does not know, one on
/// another branch, or one ahead of the default branch is never reported as out of date.
/// </remarks>
public static class AiUpdateCheck
{
    /// <summary>
    /// The GitHub repository the corpus is downloaded from.
    /// </summary>
    public const string GitHubRepository = "Cratis/AI";

    /// <summary>
    /// The prefix of the keys a comparison is cached under; the installed commit follows it.
    /// </summary>
    public const string CacheKeyPrefix = $"github:{GitHubRepository}:compare:";

    /// <summary>
    /// The environment variable 'cratis ai' reads a local corpus checkout from.
    /// </summary>
    public const string SourceEnvVar = "CRATIS_AI_SOURCE";

    const string ConfigurationPath = ".cratis/ai.json";
    const string ManifestPath = ".cratis/ai.manifest.json";
    const int ShortRevisionLength = 7;
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
    static readonly string[] _manifestCommands = ["install", "update", "uninstall"];

    /// <summary>
    /// Checks whether a newer corpus is available for the Cratis AI installation in a project.
    /// </summary>
    /// <param name="projectPath">The directory 'cratis ai update' would run in.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The update that is available, or null when there is none, nothing is installed, or the check failed.</returns>
    public static async Task<AiCorpusUpdate?> CheckForUpdate(string projectPath, CancellationToken cancellationToken = default)
    {
        if (UpdateChecker.IsDisabled())
        {
            return null;
        }

        try
        {
            var installed = InstalledRevision(projectPath);
            if (installed is null || !ShouldCheck(installed, Environment.GetEnvironmentVariable(SourceEnvVar)))
            {
                return null;
            }

            return await Check(new UpdateCheckCache(UpdateChecker.GetCachePath()), installed, token => Compare(installed, token), cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // An installation whose files cannot be read is not something a startup hint should fail over; the
            // ai commands themselves report it when run.
            return null;
        }
    }

    /// <summary>
    /// Decides whether the check applies to a command line at all.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>False for the ai commands that rewrite the manifest, true otherwise.</returns>
    /// <remarks>
    /// Install, update and uninstall replace the revision the check compared against while they run, so an
    /// answer read before them would describe an installation that no longer exists.
    /// </remarks>
    public static bool AppliesTo(string[] args) =>
        !(args.Length > 1 &&
          string.Equals(args[0], "ai", StringComparison.OrdinalIgnoreCase) &&
          _manifestCommands.Contains(args[1], StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the hint shown after a command when a newer corpus is available.
    /// </summary>
    /// <param name="update">The available update.</param>
    /// <returns>A user-facing hint message.</returns>
    public static string GetUpdateHint(AiCorpusUpdate update) =>
        $"Cratis AI update available: {update.NewCommits} new commit{(update.NewCommits == 1 ? string.Empty : "s")} since {update.InstalledRevision[..ShortRevisionLength]} - run 'cratis ai update'";

    /// <summary>
    /// Gets the key the comparison for an installed commit is cached under.
    /// </summary>
    /// <param name="installed">The revision recorded as installed.</param>
    /// <returns>The cache key.</returns>
    /// <remarks>
    /// The key names the commit so that moving to another installation - a different project, or the same one
    /// after 'cratis ai update' - never inherits the freshness or backoff of a comparison made for another commit.
    /// </remarks>
    internal static string CacheKeyFor(string installed) => $"{CacheKeyPrefix}{installed.ToLowerInvariant()}";

    /// <summary>
    /// Checks whether a newer corpus is available for an installed commit, through the given cache.
    /// </summary>
    /// <param name="cache">The cache to read and record comparisons in.</param>
    /// <param name="installed">The revision recorded as installed.</param>
    /// <param name="compare">Compares the installed revision with the default branch; returns the cached value form.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The update that is available, or null when there is none or the check failed.</returns>
    /// <remarks>Storing a comparison drops the one kept for any other commit, so the cache holds one at most.</remarks>
    internal static async Task<AiCorpusUpdate?> Check(UpdateCheckCache cache, string installed, Func<CancellationToken, Task<string?>> compare, CancellationToken cancellationToken)
    {
        var latest = await CachedVersionCheck.Check(cache, CacheKeyFor(installed), installed, false, compare, IsNewer, cancellationToken, CacheKeyPrefix);
        return latest is null ? null : FromCacheValue(latest);
    }

    /// <summary>
    /// Decides whether a project's installation can be compared against the published corpus at all.
    /// </summary>
    /// <param name="installedRevision">The revision recorded in the manifest, or null when nothing is installed.</param>
    /// <param name="sourceOverride">The value of <see cref="SourceEnvVar"/>, when set.</param>
    /// <returns>True when a check is worth making.</returns>
    /// <remarks>
    /// A local checkout named through <see cref="SourceEnvVar"/> is what 'cratis ai update' installs from, so the
    /// published corpus says nothing about it. A revision that is not a commit came from a folder that is not a
    /// Git checkout, and cannot be compared either.
    /// </remarks>
    internal static bool ShouldCheck(string? installedRevision, string? sourceOverride) =>
        string.IsNullOrEmpty(sourceOverride) && IsCommit(installedRevision);

    /// <summary>
    /// Determines whether a cached comparison reports an update for the revision installed now.
    /// </summary>
    /// <param name="cached">The comparison recorded in the cache.</param>
    /// <param name="installed">The revision recorded as installed.</param>
    /// <returns>True when the comparison was made for this revision and found new commits.</returns>
    /// <remarks>A comparison made for an earlier installation says nothing about the current one.</remarks>
    internal static bool IsNewer(string cached, string installed) =>
        FromCacheValue(cached) is { NewCommits: > 0 } update &&
        string.Equals(update.InstalledRevision, installed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads a GitHub comparison of the installed revision (base) with the default branch (head).
    /// </summary>
    /// <param name="installed">The revision recorded as installed.</param>
    /// <param name="json">The comparison response.</param>
    /// <returns>The comparison, with no new commits unless the branch is strictly ahead; null when the response cannot be read.</returns>
    /// <remarks>
    /// GitHub describes the head relative to the base: "ahead" means the default branch holds commits the
    /// installed revision does not, and none the other way round. "identical", "behind" and "diverged" all mean
    /// 'cratis ai update' would not simply bring in newer commits.
    /// </remarks>
    internal static AiCorpusUpdate? ParseComparison(string installed, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return status.GetString() switch
        {
            "ahead" when root.TryGetProperty("ahead_by", out var aheadBy) && aheadBy.TryGetInt32(out var newCommits) => new(installed, newCommits),
            "identical" or "behind" or "diverged" => new(installed, 0),
            _ => null
        };
    }

    /// <summary>
    /// Formats a comparison for the cache.
    /// </summary>
    /// <param name="update">The comparison.</param>
    /// <returns>The cached value.</returns>
    internal static string ToCacheValue(AiCorpusUpdate update) => $"{update.InstalledRevision}:{update.NewCommits}";

    /// <summary>
    /// Reads a comparison from the cache.
    /// </summary>
    /// <param name="value">The cached value.</param>
    /// <returns>The comparison, or null when the value is not one.</returns>
    internal static AiCorpusUpdate? FromCacheValue(string value)
    {
        var parts = value.Split(':');
        return parts.Length == 2 && IsCommit(parts[0]) && int.TryParse(parts[1], out var newCommits) && newCommits >= 0
            ? new(parts[0], newCommits)
            : null;
    }

    /// <summary>
    /// Reads the revision recorded as installed in a project, without changing anything.
    /// </summary>
    /// <param name="projectPath">The project directory.</param>
    /// <returns>The recorded revision, or null when the project has no Cratis AI installation.</returns>
    internal static string? InstalledRevision(string projectPath)
    {
        var configuration = Path.Combine(projectPath, ConfigurationPath);
        var manifest = Path.Combine(projectPath, ManifestPath);
        if (!File.Exists(configuration) || !File.Exists(manifest))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        return document.RootElement.ValueKind == JsonValueKind.Object &&
               document.RootElement.TryGetProperty(nameof(AiInstallationManifest.SourceRevision), out var revision) &&
               revision.ValueKind == JsonValueKind.String
            ? revision.GetString()
            : null;
    }

    static bool IsCommit(string? revision) =>
        revision is { Length: 40 } && revision.All(char.IsAsciiHexDigit);

    static async Task<string?> Compare(string installed, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = _timeout };

        // GitHub rejects requests without a user agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Cratis.Cli");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        // Only the summary is wanted. The changed files come on the first page only and the commits on their own
        // pages, so asking for a page far past the end leaves a response of a few kilobytes instead of a megabyte.
        var url = $"https://api.github.com/repos/{GitHubRepository}/compare/{installed}...HEAD?per_page=1&page=1000";
        var response = await http.GetAsync(url, cancellationToken);
        LatestVersion.ThrowIfRateLimited(response, GitHubRepository);

        // GitHub does not know a commit that was never pushed to Cratis/AI - nothing newer can be said about it.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ToCacheValue(new(installed, 0));
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var comparison = ParseComparison(installed, await response.Content.ReadAsStringAsync(cancellationToken));
        return comparison is null ? null : ToCacheValue(comparison);
    }
}
