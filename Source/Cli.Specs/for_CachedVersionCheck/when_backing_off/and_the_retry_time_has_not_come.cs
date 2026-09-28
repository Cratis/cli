// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_backing_off;

public class and_the_retry_time_has_not_come : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = CachedVersionCheck.IsBackingOff(new UpdateCheckEntry(null, default, _now.AddMinutes(5)), _now);

    [Fact] void should_not_ask_the_source() => _result.ShouldBeTrue();
}
