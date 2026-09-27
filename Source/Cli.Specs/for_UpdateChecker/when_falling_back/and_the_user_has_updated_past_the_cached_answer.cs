// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_falling_back;

/// <summary>
/// The cached answer is now behind what is installed, so it says nothing about what comes next.
/// </summary>
public class and_the_user_has_updated_past_the_cached_answer : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = UpdateChecker.IsUsableFallback("3.19.0", _now.AddHours(-2), "3.20.0", _now, UpdateChecker.IsNewer);

    [Fact] void should_not_serve_the_cached_answer() => _result.ShouldBeFalse();
}
