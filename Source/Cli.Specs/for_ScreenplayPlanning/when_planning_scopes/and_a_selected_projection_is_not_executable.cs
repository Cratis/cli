// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_a_selected_projection_is_not_executable : given.a_model_with_an_unsupported_projection
{
    ScreenplayRenderPlan[] _results = null!;

    async Task Because() => _results =
    [
        await Plan("Projects"),
        await Plan("Projects.Registration"),
        await Plan("Projects.Registration.ProjectLookup"),
        await Plan("Tasks", "Projects.Registration.ProjectLookup")
    ];

    [Fact] void should_report_the_plan_issue_as_a_warning() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning && diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT")).ShouldBeTrue();
    [Fact] void should_not_report_the_plan_issue_as_an_error() => _results.SelectMany(result => result.Diagnostics).Where(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_hand_every_model_to_the_renderer() => _results.All(result => result.Artifacts is not null).ShouldBeTrue();
}
