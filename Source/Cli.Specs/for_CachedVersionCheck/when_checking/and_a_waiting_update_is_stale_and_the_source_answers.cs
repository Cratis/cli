// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

/// <summary>
/// The case behind #191: 3.19.0 was found earlier, 3.20.0 has been published since, and the hint must name 3.20.0.
/// </summary>
public class and_a_waiting_update_is_stale_and_the_source_answers : given.a_cache
{
    string? _result;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2)));

    async Task Because()
    {
        _result = await Check(_ => Task.FromResult<string?>("3.20.0"));
    }

    [Fact] void should_report_the_version_the_source_holds_now() => _result.ShouldEqual("3.20.0");
    [Fact] void should_record_it() => _cache.Read(Key)!.LatestVersion.ShouldEqual("3.20.0");
}
