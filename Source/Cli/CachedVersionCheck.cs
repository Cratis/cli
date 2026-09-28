// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Cli;

/// <summary>
/// Asks a source for its latest version, trusting a recent cached answer and backing off after a failure.
/// </summary>
/// <remarks>
/// Every answer is asked again within the hour, because a release makes both "up to date" and "3.19.0 is
/// available" wrong the moment it is published. An update found within the last day is still shown when the
/// source is slow or fails, and a failed attempt is not repeated until its backoff has passed, so an unreachable
/// or rate-limited source does not cost a request on every command.
/// </remarks>
internal static class CachedVersionCheck
{
    static readonly TimeSpan _revalidateInterval = TimeSpan.FromHours(1);
    static readonly TimeSpan _fallbackInterval = TimeSpan.FromHours(24);
    static readonly TimeSpan _failureBackoff = TimeSpan.FromMinutes(15);
    static readonly TimeSpan _rateLimitedBackoff = TimeSpan.FromHours(1);
    static readonly TimeSpan _revalidationGrace = TimeSpan.FromMilliseconds(250);
    static readonly ConcurrentBag<Task> _refreshes = [];

    /// <summary>
    /// Gets a task that completes once every refresh started by this process has finished.
    /// </summary>
    /// <returns>The task to wait on.</returns>
    /// <remarks>
    /// A check that served a cached update keeps asking the source in the background. That answer only reaches
    /// the cache if the refresh finishes before the process exits, so the caller waits on this - within the same
    /// short deadline it gives the hints, through <see cref="WhenSettled"/> - rather than promising a refresh it
    /// may cut off.
    /// </remarks>
    public static Task WhenRefreshed() => Task.WhenAll(_refreshes);

    /// <summary>
    /// Waits for the given checks and every refresh they start, until a shared deadline.
    /// </summary>
    /// <param name="checks">The checks to wait for.</param>
    /// <param name="deadline">Completes when waiting should stop, whether or not everything has finished.</param>
    /// <returns>A task that completes when everything has finished or the deadline has passed, whichever is first.</returns>
    /// <remarks>
    /// A check registers its refresh before it completes, and it may do so late - after serving a cached update
    /// once its grace period has passed, or after a local lookup such as the Stage image one. Waiting for the
    /// refreshes only once the checks are done is what makes sure none of them is missed; taking the list of
    /// refreshes up front would let a check register one after the list was read and have it cut off.
    /// </remarks>
    public static async Task WhenSettled(IEnumerable<Task> checks, Task deadline)
    {
        if (await Task.WhenAny(Task.WhenAll(checks), deadline) == deadline)
        {
            return;
        }

        await Task.WhenAny(WhenRefreshed(), deadline);
    }

    /// <summary>
    /// Determines whether a cached answer can be served without asking the source again.
    /// </summary>
    /// <param name="checkedAt">When the source last answered.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <returns>True when the cached answer is fresh enough to serve.</returns>
    /// <remarks>
    /// "An update is available" used to be held for a day, and a release published in that day went unreported:
    /// the hint kept naming the version it had seen while 'cratis update' installed the newer one (#191).
    /// </remarks>
    public static bool IsFresh(DateTime checkedAt, DateTime utcNow) =>
        utcNow - checkedAt < _revalidateInterval;

    /// <summary>
    /// Determines whether a cached answer may stand in for one the source could not give.
    /// </summary>
    /// <param name="entry">The cached entry.</param>
    /// <param name="currentVersion">The version currently installed.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <param name="isNewer">Decides whether a latest version is newer than the current one.</param>
    /// <returns>True when the cached answer still reports a waiting update and is recent enough to show.</returns>
    public static bool IsUsableFallback(UpdateCheckEntry entry, string currentVersion, DateTime utcNow, Func<string, string, bool> isNewer) =>
        entry.LatestVersion is not null &&
        utcNow - entry.CheckedAt < _fallbackInterval &&
        isNewer(entry.LatestVersion, currentVersion);

    /// <summary>
    /// Determines whether a source is still backing off after a failed attempt.
    /// </summary>
    /// <param name="entry">The cached entry.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <returns>True when the source should not be asked yet.</returns>
    public static bool IsBackingOff(UpdateCheckEntry entry, DateTime utcNow) =>
        entry.RetryAfter is { } retryAfter && utcNow < retryAfter;

    /// <summary>
    /// Gets how long to wait before asking a source again after a failed attempt.
    /// </summary>
    /// <param name="rateLimited">Whether the source refused the request because of its rate limit.</param>
    /// <returns>The backoff.</returns>
    /// <remarks>An unauthenticated GitHub rate limit resets within the hour, so asking sooner only spends it again.</remarks>
    public static TimeSpan BackoffFor(bool rateLimited) => rateLimited ? _rateLimitedBackoff : _failureBackoff;

    /// <summary>
    /// Checks a source for a newer version.
    /// </summary>
    /// <param name="cache">The cache to read and record answers in.</param>
    /// <param name="cacheKey">The key the answer is cached under.</param>
    /// <param name="currentVersion">The version currently installed.</param>
    /// <param name="bypassCache">Whether to ask the source directly rather than trusting the cached answer.</param>
    /// <param name="fetch">Reads the latest version from the source; returns null when the source did not answer.</param>
    /// <param name="isNewer">Decides whether a latest version is newer than the current one.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <param name="supersedes">A key prefix whose other entries the answer replaces, or null to keep every other entry.</param>
    /// <param name="revalidationGrace">Produces the period a stale cached update waits for the source; null for the default.</param>
    /// <returns>The latest version if newer, otherwise null.</returns>
    public static async Task<string?> Check(
        UpdateCheckCache cache,
        string cacheKey,
        string currentVersion,
        bool bypassCache,
        Func<CancellationToken, Task<string?>> fetch,
        Func<string, string, bool> isNewer,
        CancellationToken cancellationToken,
        string? supersedes = null,
        Func<CancellationToken, Task>? revalidationGrace = null)
    {
        var entry = bypassCache ? null : cache.Read(cacheKey);
        var now = DateTime.UtcNow;
        var fallback = entry is not null && IsUsableFallback(entry, currentVersion, now, isNewer) ? entry.LatestVersion : null;
        if (entry?.LatestVersion is { } cachedVersion && IsFresh(entry.CheckedAt, now))
        {
            return isNewer(cachedVersion, currentVersion) ? cachedVersion : null;
        }

        if (entry is not null && IsBackingOff(entry, now))
        {
            return fallback;
        }

        // The refresh is registered before this check can complete, so a caller that waits for the check and then
        // for WhenRefreshed never misses it - see WhenSettled.
        var refresh = Refresh(cache, cacheKey, supersedes, fetch, cancellationToken);
        _refreshes.Add(refresh);

        // A waiting update is served straight away unless the source answers almost at once. The refresh keeps
        // running and records its answer if it finishes before the process exits.
        var grace = revalidationGrace?.Invoke(cancellationToken) ?? Task.Delay(_revalidationGrace, cancellationToken);
        if (fallback is not null && await Task.WhenAny(refresh, grace) != refresh)
        {
            return fallback;
        }

        var latestVersion = await refresh ?? fallback;
        return latestVersion is not null && isNewer(latestVersion, currentVersion) ? latestVersion : null;
    }

    static async Task<string?> Refresh(UpdateCheckCache cache, string cacheKey, string? supersedes, Func<CancellationToken, Task<string?>> fetch, CancellationToken cancellationToken)
    {
        string? latestVersion = null;
        var rateLimited = false;
        try
        {
            latestVersion = await fetch(cancellationToken);
        }
        catch (SourceRateLimited)
        {
            rateLimited = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException)
        {
            // An unreachable, slow or garbled source is recorded as a failure below and never surfaces: a missing
            // update hint must not fail the command it follows.
        }

        var now = DateTime.UtcNow;
        cache.Store(
            cacheKey,
            existing => latestVersion is not null
                ? new UpdateCheckEntry(latestVersion, now)
                : new UpdateCheckEntry(existing?.LatestVersion, existing?.CheckedAt ?? default, now + BackoffFor(rateLimited)),
            supersedes);
        return latestVersion;
    }
}
