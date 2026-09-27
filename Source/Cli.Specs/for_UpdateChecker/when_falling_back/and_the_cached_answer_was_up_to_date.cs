// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_falling_back;

/// <summary>
/// Up to date stops being true the moment a release happens, so it is never worth serving in place of the source.
/// </summary>
public class and_the_cached_answer_was_up_to_date : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = UpdateChecker.IsUsableFallback("3.18.0", _now.AddMinutes(-90), "3.18.0", _now, UpdateChecker.IsNewer);

    [Fact] void should_not_serve_the_cached_answer() => _result.ShouldBeFalse();
}
