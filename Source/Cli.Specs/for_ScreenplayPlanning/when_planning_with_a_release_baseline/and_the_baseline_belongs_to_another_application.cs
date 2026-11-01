// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_the_baseline_belongs_to_another_application : given.a_release_baseline
{
    void Establish() => _request = _request with { ReleaseBaseline = new FileReleaseBaseline(ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(_baseline.Document!), ApplicationIdentity.Create("AnotherApplication"))) };
    void Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_application_mismatch() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-003");
}
