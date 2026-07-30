// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Checks whether a newer version is available and caches the result, so that the check runs at most once per
/// interval rather than on every command.
/// </summary>
/// <remarks>
/// The CLI's own version is read from wherever the running installation updates from - NuGet for a dotnet tool,
/// the GitHub releases for the native downloads - while other packages are read from NuGet.
/// </remarks>
public static class UpdateChecker
{
    /// <summary>
    /// The NuGet package ID for the CLI tool.
    /// </summary>
    public const string CliPackageId = "Cratis.Cli";

    /// <summary>
    /// The NuGet package ID used as a proxy for the Chronicle server version.
    /// The server ships as a Docker image but shares the same release version as this client library.
    /// </summary>
    public const string ServerPackageId = "Cratis.Chronicle";

    /// <summary>
    /// Environment variable that disables the update check entirely when set to any non-empty value.
    /// </summary>
    public const string DisableEnvVar = "CRATIS_NO_UPDATE_CHECK";

    static readonly TimeSpan _updateAvailableInterval = TimeSpan.FromHours(24);
    static readonly TimeSpan _upToDateInterval = TimeSpan.FromHours(1);
    static readonly JsonSerializerOptions _cacheJsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Returns true when the update check has been switched off through <see cref="DisableEnvVar"/>.
    /// </summary>
    /// <returns>True when the check should be skipped.</returns>
    public static bool IsDisabled() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DisableEnvVar));

    /// <summary>
    /// Gets the path to the cached version check file.
    /// </summary>
    /// <returns>The absolute file path.</returns>
    public static string GetCachePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cratis", "version-check.json");

    /// <summary>
    /// Checks whether a newer version of the CLI is available, from the place this installation updates from.
    /// </summary>
    /// <param name="currentVersion">The current CLI version.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version string if newer, otherwise null.</returns>
    public static Task<string?> CheckForUpdate(string currentVersion, CancellationToken cancellationToken = default)
        => CheckForCliUpdate(currentVersion, false, cancellationToken);

    /// <summary>
    /// Checks whether a newer version of the CLI is available, from the place this installation updates from.
    /// </summary>
    /// <param name="currentVersion">The current CLI version.</param>
    /// <param name="bypassCache">Whether to ask the source directly rather than trusting the cached answer.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version string if newer, otherwise null.</returns>
    /// <remarks>
    /// A dotnet tool is compared against NuGet and a native installation against the GitHub releases, because
    /// those are published separately and comparing against the wrong one reports an update that the user's own
    /// update command cannot yet install, or none when one is waiting.
    /// </remarks>
    public static async Task<string?> CheckForCliUpdate(string currentVersion, bool bypassCache, CancellationToken cancellationToken = default)
    {
        var source = LatestVersion.SourceFor(CliUpdate.DetectStrategy());
        return await Check(
            CacheKeyFor(source, CliPackageId),
            currentVersion,
            bypassCache,
            token => source == LatestVersionSource.NuGet
                ? LatestVersion.FromNuGet(CliPackageId, token)
                : LatestVersion.FromGitHubRelease(token),
            cancellationToken);
    }

    /// <summary>
    /// Checks whether a newer version of the specified NuGet package is available.
    /// Returns the latest version string if an update is available, or null if the
    /// package is up to date or the check fails. Designed to be called with a short
    /// timeout so it never blocks the user.
    /// </summary>
    /// <param name="packageId">The NuGet package ID to check.</param>
    /// <param name="currentVersion">The current version.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version string if newer, otherwise null.</returns>
    public static Task<string?> CheckForUpdate(string packageId, string currentVersion, CancellationToken cancellationToken = default) =>
        Check(packageId, currentVersion, false, token => LatestVersion.FromNuGet(packageId, token), cancellationToken);

    /// <summary>
    /// Gets the cache key an answer read from the given source is stored under.
    /// </summary>
    /// <param name="source">The source the answer came from.</param>
    /// <param name="packageId">The package identifier, used when reading from NuGet.</param>
    /// <returns>The cache key.</returns>
    /// <remarks>
    /// The sources are keyed apart so an answer read from one is never served for the other - the same
    /// installation can change how it updates, and the two do not publish at the same moment.
    /// </remarks>
    internal static string CacheKeyFor(LatestVersionSource source, string packageId) =>
        source == LatestVersionSource.NuGet ? packageId : $"github:{LatestVersion.GitHubRepository}";

    /// <summary>
    /// Determines whether a cached answer can still be trusted.
    /// </summary>
    /// <param name="cachedLatestVersion">The latest version recorded when the check ran.</param>
    /// <param name="checkedAt">When the check ran.</param>
    /// <param name="currentVersion">The version currently installed.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <returns>True when the cached answer is still fresh enough to serve.</returns>
    /// <remarks>
    /// The two answers do not go stale at the same rate. "An update is available" stays true until the user
    /// updates, so it is held for a day. "You are up to date" stops being true the moment a release happens,
    /// and a check landing in the window between the release and the package becoming visible records the
    /// previous version - holding that for a day means the release goes unannounced for a day.
    /// </remarks>
    internal static bool IsFresh(string cachedLatestVersion, DateTime checkedAt, string currentVersion, DateTime utcNow) =>
        utcNow - checkedAt < (IsNewer(cachedLatestVersion, currentVersion) ? _updateAvailableInterval : _upToDateInterval);

    /// <summary>
    /// Determines whether the latest version is newer than the current version.
    /// </summary>
    /// <param name="latest">The latest available version.</param>
    /// <param name="current">The current version (may include prerelease suffix).</param>
    /// <returns>True if the latest version is strictly greater than the current version.</returns>
    internal static bool IsNewer(string latest, string current)
    {
        var dashIndex = current.IndexOf('-');
        var currentNumeric = dashIndex > 0 ? current[..dashIndex] : current;

        return Version.TryParse(latest, out var latestVer) &&
               Version.TryParse(currentNumeric, out var currentVer) &&
               latestVer > currentVer;
    }

    static async Task<string?> Check(
        string cacheKey,
        string currentVersion,
        bool bypassCache,
        Func<CancellationToken, Task<string?>> fetch,
        CancellationToken cancellationToken)
    {
        if (IsDisabled())
        {
            return null;
        }

        var cache = ReadCache();
        if (!bypassCache &&
            cache?.Packages.TryGetValue(cacheKey, out var entry) == true &&
            IsFresh(entry.LatestVersion, entry.CheckedAt, currentVersion, DateTime.UtcNow))
        {
            return IsNewer(entry.LatestVersion, currentVersion) ? entry.LatestVersion : null;
        }

        try
        {
            var latestVersion = await fetch(cancellationToken);
            if (latestVersion is null)
            {
                return null;
            }

            cache ??= new VersionCache();
            cache.Packages[cacheKey] = new PackageVersionEntry(latestVersion, DateTime.UtcNow);
            WriteCache(cache);

            return IsNewer(latestVersion, currentVersion) ? latestVersion : null;
        }
        catch
        {
            return null;
        }
    }

    static VersionCache? ReadCache()
    {
        var path = GetCachePath();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<VersionCache>(json);
        }
        catch
        {
            return null;
        }
    }

    static void WriteCache(VersionCache cache)
    {
        try
        {
            var path = GetCachePath();
            var directory = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(cache, _cacheJsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Cache write failure is non-critical.
        }
    }

    sealed record VersionCache
    {
        public Dictionary<string, PackageVersionEntry> Packages { get; set; } = [];
    }

    sealed record PackageVersionEntry(string LatestVersion, DateTime CheckedAt);
}
