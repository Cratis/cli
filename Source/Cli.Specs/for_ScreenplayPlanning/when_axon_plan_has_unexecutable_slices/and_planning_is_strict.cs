// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_axon_plan_has_unexecutable_slices;

public class and_planning_is_strict : given.an_axon_target_with_an_unsupported_projection
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "axon") { Strict = true }, CancellationToken.None);

    [Fact] void should_refuse_the_plan() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_plan_issue_as_an_error() => _result.Diagnostics.Single(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Code.ShouldEqual("PLAN-UNSUPPORTEDEVENTCONTEXT");
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_not_invoke_the_axon_rendering() => _rendering.DidNotReceive().Plan(Arg.Any<Cratis.Cli.Commands.Render.Axon.AxonRenderInput>());
}
