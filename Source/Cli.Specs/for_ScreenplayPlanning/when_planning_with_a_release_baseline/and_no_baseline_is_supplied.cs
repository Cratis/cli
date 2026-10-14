// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_no_baseline_is_supplied : given.a_release_baseline
{
    void Establish() => _request = _request with { ReleaseBaseline = null };
    void Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_baseline_warnings() => _result.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("STAGE-BASELINE-", StringComparison.Ordinal)).ShouldBeEmpty();
}
