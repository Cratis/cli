// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_cratis_plan_has_unexecutable_slices;

public class and_planning_is_not_strict : given.a_cratis_target_with_an_unsupported_projection
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await Plan();

    [Fact] void should_report_the_plan_issue_as_a_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_not_report_a_plan_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code.StartsWith("PLAN-", StringComparison.Ordinal) && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_hand_the_model_to_the_renderer() => _result.Artifacts.ShouldNotBeNull();
}
