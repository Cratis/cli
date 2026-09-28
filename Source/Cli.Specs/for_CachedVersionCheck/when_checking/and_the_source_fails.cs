// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking;

public class and_the_source_fails : given.a_cache
{
    string? _result;

    async Task Because()
    {
        _result = await Check(_ => throw new HttpRequestException("unreachable"));
    }

    [Fact] void should_report_nothing() => _result.ShouldBeNull();
    [Fact] void should_back_off() => _cache.Read(Key)!.RetryAfter!.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(10));
    [Fact] void should_not_back_off_as_long_as_for_a_rate_limit() => _cache.Read(Key)!.RetryAfter!.Value.ShouldBeLessThan(DateTime.UtcNow.AddMinutes(20));
}
