// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_falling_back;

public class and_an_update_has_been_waiting_over_a_day : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = CachedVersionCheck.IsUsableFallback(new UpdateCheckEntry("3.19.0", _now.AddHours(-25)), "3.18.0", _now, UpdateChecker.IsNewer);

    [Fact] void should_not_serve_the_cached_update() => _result.ShouldBeFalse();
}
