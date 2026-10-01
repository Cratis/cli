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
/// caller never waits on the refreshes another one started.
/// </remarks>
public sealed class VersionRefreshes
{
    readonly ConcurrentBag<Task> _refreshes = [];

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
