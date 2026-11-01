// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_cratis_plan_has_unexecutable_slices;

public class and_planning_is_strict : given.a_cratis_target_with_an_unsupported_projection
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "cratis") { Strict = true }, CancellationToken.None);

    [Fact] void should_refuse_the_plan() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_plan_issue_as_an_error() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
}
