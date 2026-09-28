// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_falling_back;

/// <summary>
/// A slow or unreachable source should not hide an update that is still waiting.
/// </summary>
public class and_an_update_was_found_within_the_day : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = CachedVersionCheck.IsUsableFallback(new UpdateCheckEntry("3.19.0", _now.AddHours(-12)), "3.18.0", _now, UpdateChecker.IsNewer);

    [Fact] void should_serve_the_cached_update() => _result.ShouldBeTrue();
}
