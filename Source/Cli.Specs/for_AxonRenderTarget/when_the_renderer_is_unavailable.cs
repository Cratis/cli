// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_the_renderer_is_unavailable : given.an_axon_target
{
    ArtifactRenderPlan _result = null!;

    void Because() => _result = new AxonRenderTarget(new UnavailableAxonRendering()).Plan(
        _compilation, _execution, null, null, [], [], [], [], _warnings);

    [Fact] void should_refuse_publication() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.Length.ShouldEqual(0);
    [Fact] void should_explain_the_missing_package() => _result.Diagnostics.Single().Code.ShouldEqual("CLI-RENDER-028");
}
