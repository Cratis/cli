// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_axon_ignores_strict_acceptance : for_AxonRenderTarget.given.an_axon_target
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([_target]));

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "axon") { Strict = true }, CancellationToken.None);

    [Fact] void should_keep_the_plan_publishable() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_ignored_option() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "CLI-RENDER-030").Message.ShouldContain("--strict");
    [Fact] void should_keep_the_warning_in_planning_diagnostics() => _result.PlanningDiagnostics!.Single(diagnostic => diagnostic.Code == "CLI-RENDER-030").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_not_apply_strict_acceptance_to_the_renderer() => _input.Strict.ShouldBeFalse();
}
