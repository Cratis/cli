// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_checking_freshness;

/// <summary>
/// An update stays available until the user installs it, so this answer survives far longer.
/// </summary>
public class and_an_update_is_waiting : Specification
{
    static readonly DateTime _now = new(2026, 7, 30, 20, 0, 0, DateTimeKind.Utc);

    bool _result;

    void Because() => _result = UpdateChecker.IsFresh("2.3.6", _now.AddHours(-12), "2.3.4", _now);

    [Fact] void should_still_be_trusted() => _result.ShouldBeTrue();
}
