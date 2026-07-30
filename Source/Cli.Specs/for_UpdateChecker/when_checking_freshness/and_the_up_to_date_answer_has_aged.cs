// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_checking_freshness;

/// <summary>
/// A check that lands in the window between a release being tagged and the package becoming visible records
/// the previous version. Trusting that for a day means every release published after it goes unannounced for
/// a day, which is exactly what happened between 2.3.4 and 2.3.6.
/// </summary>
public class and_the_up_to_date_answer_has_aged : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = UpdateChecker.IsFresh("2.3.4", _now.AddHours(-2), "2.3.4", _now);

    [Fact] void should_no_longer_be_trusted() => _result.ShouldBeFalse();
}
