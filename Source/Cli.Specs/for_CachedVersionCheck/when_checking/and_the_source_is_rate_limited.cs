// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

public class and_the_source_is_rate_limited : given.a_cache
{
    string? _result;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2)));

    async Task Because()
    {
        _result = await Check(_ => throw new SourceRateLimited("Cratis/cli"));
    }

    [Fact] void should_report_the_cached_update() => _result.ShouldEqual("3.19.0");
    [Fact] void should_back_off_until_the_limit_resets() => _cache.Read(Key)!.RetryAfter!.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(50));
    [Fact] void should_keep_the_last_answer() => _cache.Read(Key)!.LatestVersion.ShouldEqual("3.19.0");
}
