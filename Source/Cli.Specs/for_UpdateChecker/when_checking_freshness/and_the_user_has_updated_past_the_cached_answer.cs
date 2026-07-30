// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_checking_freshness;

/// <summary>
/// The cached answer is now behind what is installed, so it says nothing about what comes next.
/// </summary>
public class and_the_user_has_updated_past_the_cached_answer : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = UpdateChecker.IsFresh("2.3.4", _now.AddHours(-2), "2.3.6", _now);

    [Fact] void should_no_longer_be_trusted() => _result.ShouldBeFalse();
}
