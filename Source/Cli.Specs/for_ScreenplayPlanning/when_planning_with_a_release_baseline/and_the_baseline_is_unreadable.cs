// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_the_baseline_is_unreadable : given.a_release_baseline
{
    void Establish() => _request = _request with { ReleaseBaseline = new FileReleaseBaseline(new("unreadable", null, null, "malformed baseline")) };
    void Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_unreadable_baseline() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-003" && diagnostic.Message.Contains("malformed baseline", StringComparison.Ordinal));
}
