// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_the_render_target_rejects_the_artifact_contract : given.a_failing_render_target
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => _failure = new InvalidArtifactRenderContract("Invalid artifact contract.");

    void Because() => _result = _planning.PlanDocuments(_request, CancellationToken.None);

    [Fact] void should_refuse_the_render() => _result.Success.ShouldBeFalse();
    [Fact] void should_preserve_the_expected_diagnostic() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "CLI-RENDER-005");
    [Fact] void should_have_no_publishable_artifacts() => _result.Artifacts.ShouldBeNull();
}
