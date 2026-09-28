// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_checking_freshness;

public class and_the_up_to_date_answer_is_recent : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = CachedVersionCheck.IsFresh(_now.AddMinutes(-30), _now);

    [Fact] void should_still_be_trusted() => _result.ShouldBeTrue();
}
