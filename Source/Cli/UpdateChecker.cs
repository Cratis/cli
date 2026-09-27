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
    /// The NuGet package ID used as a proxy for the Stage image version.
    /// The Stage sandbox ships as a Docker image but shares the same release version as this contracts library.
    /// </summary>
    public const string StagePackageId = "Cratis.Stage.Contracts";

    /// <summary>
    /// Environment variable that disables the update check entirely when set to any non-empty value.
    /// </summary>
    public const string DisableEnvVar = "CRATIS_NO_UPDATE_CHECK";

    static readonly TimeSpan _revalidateInterval = TimeSpan.FromHours(1);
    static readonly TimeSpan _fallbackInterval = TimeSpan.FromHours(24);
    static readonly TimeSpan _revalidationGrace = TimeSpan.FromMilliseconds(250);
    static readonly Lock _cacheLock = new();
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
            IsNewer,
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
        CheckForUpdate(packageId, currentVersion, false, cancellationToken);

    /// <summary>
    /// Checks whether a newer version of the specified NuGet package is available.
    /// Returns the latest version string if an update is available, or null if the
    /// package is up to date or the check fails. Designed to be called with a short
    /// timeout so it never blocks the user.
    /// </summary>
    /// <param name="packageId">The NuGet package ID to check.</param>
    /// <param name="currentVersion">The current version.</param>
    /// <param name="bypassCache">Whether to ask NuGet directly rather than trusting the cached answer.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version string if newer, otherwise null.</returns>
    public static Task<string?> CheckForUpdate(string packageId, string currentVersion, bool bypassCache, CancellationToken cancellationToken = default) =>
        Check(packageId, currentVersion, bypassCache, token => LatestVersion.FromNuGet(packageId, token), IsNewer, cancellationToken);

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
    /// Determines whether a cached answer can still be served without asking the source again.
    /// </summary>
    /// <param name="checkedAt">When the check ran.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <returns>True when the cached answer is still fresh enough to serve.</returns>
    /// <remarks>
    /// Both answers go stale the moment a release happens, so both are re-checked within the hour. "An update
    /// is available" used to be held for a day, and a release published in that day went unreported: the hint
    /// kept naming the version it had seen while 'cratis update' installed the newer one.
    /// </remarks>
    internal static bool IsFresh(DateTime checkedAt, DateTime utcNow) =>
        utcNow - checkedAt < _revalidateInterval;

    /// <summary>
    /// Determines whether a cached answer may stand in for one the source could not give in time.
    /// </summary>
    /// <param name="cachedLatestVersion">The latest version recorded when the check ran.</param>
    /// <param name="checkedAt">When the check ran.</param>
    /// <param name="currentVersion">The version currently installed.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <param name="isNewer">Decides whether the cached latest version is newer than the current version.</param>
    /// <returns>True when the cached answer still reports a waiting update and is recent enough to show.</returns>
    /// <remarks>
    /// Only "an update is available" is worth falling back to - it stays true until the user updates, and a
    /// slow or unreachable source should not hide it. The source is still asked, so the next run is correct.
    /// </remarks>
    internal static bool IsUsableFallback(string cachedLatestVersion, DateTime checkedAt, string currentVersion, DateTime utcNow, Func<string, string, bool> isNewer) =>
        utcNow - checkedAt < _fallbackInterval && isNewer(cachedLatestVersion, currentVersion);

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

    /// <summary>
    /// Checks a source for a newer version or revision, trusting a recent cached answer and falling back to an
    /// older one that reported an update when the source does not answer in time.
    /// </summary>
    /// <param name="cacheKey">The key the answer is cached under.</param>
    /// <param name="currentVersion">The version or revision currently installed.</param>
    /// <param name="bypassCache">Whether to ask the source directly rather than trusting the cached answer.</param>
    /// <param name="fetch">Reads the latest version or revision from the source.</param>
    /// <param name="isNewer">Decides whether a latest version is newer than the current one.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version or revision if newer, otherwise null.</returns>
    internal static async Task<string?> Check(
        string cacheKey,
        string currentVersion,
        bool bypassCache,
        Func<CancellationToken, Task<string?>> fetch,
        Func<string, string, bool> isNewer,
        CancellationToken cancellationToken)
    {
        if (IsDisabled())
        {
            return null;
        }

        var cache = ReadCache() ?? new VersionCache();
        PackageVersionEntry? entry = null;
        if (!bypassCache && cache.Packages.TryGetValue(cacheKey, out entry) && IsFresh(entry.CheckedAt, DateTime.UtcNow))
        {
            return isNewer(entry.LatestVersion, currentVersion) ? entry.LatestVersion : null;
        }

        var fallback = entry is not null && IsUsableFallback(entry.LatestVersion, entry.CheckedAt, currentVersion, DateTime.UtcNow, isNewer)
            ? entry.LatestVersion
            : null;
        var revalidation = Revalidate(cacheKey, fetch, cancellationToken);

        // Serve the waiting update straight away unless the source answers almost at once - the revalidation
        // keeps running and records its answer, so the next run reports what the source holds now.
        if (fallback is not null && await Task.WhenAny(revalidation, Task.Delay(_revalidationGrace, cancellationToken)) != revalidation)
        {
            return fallback;
        }

        var latestVersion = await revalidation ?? fallback;
        return latestVersion is not null && isNewer(latestVersion, currentVersion) ? latestVersion : null;
    }

    static async Task<string?> Revalidate(string cacheKey, Func<CancellationToken, Task<string?>> fetch, CancellationToken cancellationToken)
    {
        try
        {
            var latestVersion = await fetch(cancellationToken);
            if (latestVersion is not null)
            {
                Store(cacheKey, latestVersion);
            }

            return latestVersion;
        }
        catch
        {
            return null;
        }
    }

    static void Store(string cacheKey, string latestVersion)
    {
        // Several checks run side by side at startup, each owning one key. Re-reading under the lock keeps one
        // check's answer from overwriting another's with the copy it read before either had written.
        lock (_cacheLock)
        {
            var cache = ReadCache() ?? new VersionCache();
            cache.Packages[cacheKey] = new PackageVersionEntry(latestVersion, DateTime.UtcNow);
            WriteCache(cache);
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
