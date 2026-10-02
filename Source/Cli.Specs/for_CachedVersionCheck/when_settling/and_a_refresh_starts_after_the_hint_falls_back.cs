// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_settling;

/// <summary>
/// A fast command waits for its checks; one serves a cached update once its grace period passes while the source
/// is still answering. The refresh it leaves running must still be waited for within the same deadline.
/// </summary>
public class and_a_refresh_starts_after_the_hint_falls_back : given.a_cache
{
    readonly TaskCompletionSource _grace = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<string?> _source = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _deadline = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly VersionRefreshes _refreshes = new();
    string? _result;
    bool _settledBeforeTheSourceAnswered;
    Task _settled = null!;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2)));

    async Task Because()
    {
        var check = CachedVersionCheck.Check(_cache, Key, "3.18.0", false, _ => _source.Task, UpdateChecker.IsNewer, CancellationToken.None, revalidationGrace: _ => _grace.Task, refreshes: _refreshes);
        _settled = CachedVersionCheck.WhenSettled([check], _deadline.Task, _refreshes);

        _grace.SetResult();
        _result = await check;
        _settledBeforeTheSourceAnswered = _settled.IsCompleted;

        _source.SetResult("3.20.0");
        await _settled;
    }

    [Fact] void should_report_the_cached_update() => _result.ShouldEqual("3.19.0");
    [Fact] void should_keep_waiting_after_the_check_has_answered() => _settledBeforeTheSourceAnswered.ShouldBeFalse();
    [Fact] void should_settle_before_the_deadline() => _deadline.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_record_the_refreshed_answer() => _cache.Read(Key)!.LatestVersion.ShouldEqual("3.20.0");
}
