// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_backing_off;

public class and_the_last_attempt_succeeded : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = CachedVersionCheck.IsBackingOff(new UpdateCheckEntry("3.20.0", _now.AddHours(-2)), _now);

    [Fact] void should_ask_the_source() => _result.ShouldBeFalse();
}
