// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions;

public class through_the_axon_failure_guard : given.an_excluded_slice
{
    ArtifactRenderPlan _result = null!;

    void Because()
    {
        var rendering = Substitute.For<IAxonRendering>();
        rendering.Plan(Arg.Any<AxonRenderInput>()).Returns(_ => throw new InvalidOperationException("renderer failure"));
        var compilation = _expected.Compilation.Value;
        var execution = SemanticExecutionPlan.Compile(compilation.Model).Plan;
        _result = new AxonRenderTarget(rendering).Plan(compilation, execution, null, null, [], [], [], [], [], exclusions: _expected.Exclusions);
    }

    [Fact] void should_preserve_exclusions_in_the_cli_owned_fallback_request() => AssertStageExclusions(_result.Exclusions, _expected.Exclusions);
    [Fact] void should_remain_non_publishable() => _result.CanPublishArtifacts.ShouldBeFalse();
    [Fact] void should_retain_the_renderer_failure() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == AxonRenderingGuard.RendererFailureCode);
}
