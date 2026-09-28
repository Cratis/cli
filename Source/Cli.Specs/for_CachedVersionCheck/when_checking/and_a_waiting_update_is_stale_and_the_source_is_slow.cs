// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

/// <summary>
/// The cached update is shown rather than nothing, and the refresh still records the newer answer for the next run.
/// </summary>
public class and_a_waiting_update_is_stale_and_the_source_is_slow : given.a_cache
{
    string? _result;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2)));

    async Task Because()
    {
        _result = await Check(async _ =>
        {
            await Task.Delay(1000);
            return "3.20.0";
        });
        await CachedVersionCheck.WhenRefreshed();
    }

    [Fact] void should_report_the_cached_update() => _result.ShouldEqual("3.19.0");
    [Fact] void should_record_the_refreshed_answer() => _cache.Read(Key)!.LatestVersion.ShouldEqual("3.20.0");
}
