// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

/// <summary>
/// A failed source is not asked again on every command.
/// </summary>
public class and_the_source_is_backing_off : given.a_cache
{
    string? _result;

    void Establish() => _cache.Store(Key, _ => new UpdateCheckEntry("3.19.0", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddMinutes(10)));

    async Task Because()
    {
        _result = await Check(_ => Task.FromResult<string?>("3.20.0"));
    }

    [Fact] void should_not_ask_the_source() => _fetches.ShouldEqual(0);
    [Fact] void should_report_the_cached_update() => _result.ShouldEqual("3.19.0");
}
