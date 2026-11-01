// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_a_selected_projection_is_not_executable_in_strict_mode : given.a_model_with_an_unsupported_projection
{
    ScreenplayRenderPlan[] _results = null!;

    async Task Because() => _results =
    [
        await StrictPlan("Projects"),
        await StrictPlan("Projects.Registration"),
        await StrictPlan("Projects.Registration.ProjectLookup"),
        await StrictPlan("Tasks", "Projects.Registration.ProjectLookup")
    ];

    [Fact] void should_refuse_every_plan_that_selects_the_projection() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_keep_the_execution_plan_refusal() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error && diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT")).ShouldBeTrue();
    [Fact] void should_not_downgrade_the_selected_issue() => _results.SelectMany(result => result.Diagnostics).Where(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_not_plan_artifacts() => _results.All(result => result.Artifacts is null).ShouldBeTrue();
}
