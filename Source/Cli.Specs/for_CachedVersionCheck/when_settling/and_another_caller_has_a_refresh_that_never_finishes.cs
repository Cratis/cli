// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_settling;

/// <summary>
/// The refreshes belong to the caller that started the checks, so one that is still running for another caller
/// is not waited for.
/// </summary>
public class and_another_caller_has_a_refresh_that_never_finishes : given.a_cache
{
    readonly TaskCompletionSource<string?> _otherSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<string?> _source = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _deadline = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly VersionRefreshes _otherRefreshes = new();
    readonly VersionRefreshes _refreshes = new();
    Task _settled = null!;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2)));

    async Task Because()
    {
        // Another caller's refresh starts and is still waiting for its source when this caller settles.
        _ = Check(_ => _otherSource.Task, revalidationGrace: _ => Task.CompletedTask, refreshes: _otherRefreshes);

        var check = Check(_ => _source.Task, revalidationGrace: _ => Task.CompletedTask, refreshes: _refreshes);
        await check;
        _source.SetResult("3.20.0");

        _settled = CachedVersionCheck.WhenSettled([check], _deadline.Task, _refreshes);
        await _settled;
    }

    [Fact] void should_settle_without_waiting_for_the_other_callers_refresh() => _settled.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_run_out_the_deadline() => _deadline.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_leave_the_other_callers_refresh_alone() => _otherRefreshes.WhenRefreshed().IsCompleted.ShouldBeFalse();
}
