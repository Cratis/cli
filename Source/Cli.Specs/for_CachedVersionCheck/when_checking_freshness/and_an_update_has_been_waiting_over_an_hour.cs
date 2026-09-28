// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking_freshness;

/// <summary>
/// Holding "3.19.0 is available" for a day meant 3.20.0, released in that day, went unreported while
/// 'cratis update' installed it anyway (#191). A waiting update is re-checked as often as an up-to-date answer.
/// </summary>
public class and_an_update_has_been_waiting_over_an_hour : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = CachedVersionCheck.IsFresh(_now.AddHours(-12), _now);

    [Fact] void should_ask_the_source_again() => _result.ShouldBeFalse();
}
