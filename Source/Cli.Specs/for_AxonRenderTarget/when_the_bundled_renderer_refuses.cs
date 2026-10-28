// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_the_bundled_renderer_refuses : for_ScreenplayPlanning.given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "axon", RootNamespace: "java"), CancellationToken.None);

    [Fact] void should_fail_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_renderer_diagnostic() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-AXON-NAMES" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_not_plan_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
}
