// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

/// <summary>
/// The cached update is shown rather than nothing, and the refresh still records the newer answer for the next run.
/// </summary>
public class and_a_waiting_update_is_stale_and_the_source_is_slow : given.a_cache
{
    readonly TaskCompletionSource<string?> _source = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly VersionRefreshes _refreshes = new();
    string? _result;
    bool _refreshedWhenTheCachedUpdateWasShown;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2)));

    async Task Because()
    {
        // The grace period has already passed and the source has not answered, so the cached update is shown.
        _result = await Check(_ => _source.Task, revalidationGrace: _ => Task.CompletedTask, refreshes: _refreshes);
        _refreshedWhenTheCachedUpdateWasShown = _refreshes.WhenRefreshed().IsCompleted;

        _source.SetResult("3.20.0");
        await _refreshes.WhenRefreshed();
    }

    [Fact] void should_report_the_cached_update() => _result.ShouldEqual("3.19.0");
    [Fact] void should_still_be_refreshing_when_the_cached_update_was_shown() => _refreshedWhenTheCachedUpdateWasShown.ShouldBeFalse();
    [Fact] void should_record_the_refreshed_answer() => _cache.Read(Key)!.LatestVersion.ShouldEqual("3.20.0");
}
