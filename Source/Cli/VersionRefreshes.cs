// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Cli;

/// <summary>
/// The background refreshes started by a set of version checks, so whoever started the checks can wait for them.
/// </summary>
/// <remarks>
/// A check that served a cached update keeps asking the source in the background. That answer only reaches the
/// cache if the refresh finishes before the process exits, so the caller that started the checks waits on this -
/// within the same short deadline it gives the hints, through <see cref="CachedVersionCheck.WhenSettled"/> -
/// rather than promising a refresh it may cut off. The set belongs to the caller, not to the process, so one
/// caller never waits on the refreshes another one started. A command that runs underneath such a caller reaches
/// its set through <see cref="Current"/>, since commands are created by the command framework and cannot be given
/// it any other way.
/// </remarks>
public sealed class VersionRefreshes
{
    static readonly AsyncLocal<VersionRefreshes?> _current = new();

    readonly ConcurrentBag<Task> _refreshes = [];

    /// <summary>
    /// Gets the set of the caller this code runs under, or null when no caller waits for refreshes.
    /// </summary>
    /// <remarks>
    /// The value flows with the asynchronous call chain that follows <see cref="Begin"/> and is invisible to any
    /// other chain, so two callers in the same process never see each other's set.
    /// </remarks>
    public static VersionRefreshes? Current => _current.Value;

    /// <summary>
    /// Starts a new set for the calling chain and makes it the <see cref="Current"/> one for everything that chain calls.
    /// </summary>
    /// <returns>The new set.</returns>
    /// <remarks>Call this from the method that waits for the refreshes, not from a helper that runs on its own asynchronous chain.</remarks>
    public static VersionRefreshes Begin()
    {
        var refreshes = new VersionRefreshes();
        _current.Value = refreshes;
        return refreshes;
    }

    /// <summary>
    /// Gets a task that completes once every refresh registered so far has finished.
    /// </summary>
    /// <returns>The task to wait on.</returns>
    public Task WhenRefreshed() => Task.WhenAll(_refreshes);

    /// <summary>
    /// Registers a refresh to wait for.
    /// </summary>
    /// <param name="refresh">The refresh.</param>
    internal void Add(Task refresh) => _refreshes.Add(refresh);
}
