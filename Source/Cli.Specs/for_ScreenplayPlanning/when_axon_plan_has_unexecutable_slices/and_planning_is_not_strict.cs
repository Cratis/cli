// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_axon_plan_has_unexecutable_slices;

public class and_planning_is_not_strict : given.an_axon_target_with_an_unsupported_projection
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "axon"), CancellationToken.None);

    [Fact] void should_keep_the_plan_publishable() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_plan_issue_as_a_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_not_report_the_plan_issue_as_an_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_invoke_the_axon_rendering() => _rendering.Received(1).Plan(Arg.Any<Cratis.Cli.Commands.Render.Axon.AxonRenderInput>());
    [Fact] void should_plan_artifacts() => _result.Artifacts.ShouldEqual(_plan);
}
