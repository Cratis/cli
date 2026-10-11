// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_an_unselected_projection_is_not_executable : given.a_model_with_an_unsupported_projection
{
    ScreenplayRenderPlan[] _results = null!;

    async Task Because() => _results =
    [
        await Plan("Tasks"),
        await Plan("Tasks.Backlog"),
        await Plan("Tasks.Backlog.Intake"),
        await Plan("Tasks.Backlog.Intake.CaptureTask"),
        await Plan("Tasks.Backlog.Intake", "Projects.Registration.RegisterProject")
    ];

    [Fact] void should_admit_every_selected_scope() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_have_no_blocking_diagnostics() => _results.SelectMany(result => result.Diagnostics).Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_report_the_unselected_issue_once_per_plan() => _results.All(result => result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT") == 1).ShouldBeTrue();
    [Fact] void should_name_the_unselected_declaration_in_the_warning() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning && diagnostic.Location == "Projects.Registration.ProjectLookup.ProjectSummaryProjection")).ShouldBeTrue();
    [Fact] void should_plan_the_independent_selected_artifact() => _results.All(result => result.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath == "Tasks/Backlog/Intake/CaptureTask/CaptureTask.cs")).ShouldBeTrue();
    [Fact] void should_exclude_the_unsupported_projection() => _results.SelectMany(result => result.Artifacts!.Artifacts).Any(artifact => artifact.RelativePath.Contains("ProjectLookup", StringComparison.Ordinal)).ShouldBeFalse();
}
