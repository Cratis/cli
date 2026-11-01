// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class without_a_scope_and_an_unsupported_projection_in_strict_mode : given.a_model_with_an_unsupported_projection
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await StrictPlan();

    [Fact] void should_refuse_the_full_plan() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_execution_plan_refusal() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAN-UNSUPPORTEDEVENTCONTEXT");
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
}
