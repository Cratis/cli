// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions;

public class through_the_packaged_axon_adapter : given.an_excluded_slice
{
    ScreenplayRenderPlan _result = null!;

    void Because()
    {
        var planning = new ScreenplayPlanning(new SemanticModelCompiler(), new RenderTargetRoster([new AxonRenderTarget(new PackagedAxonRendering())]));
        _result = planning.PlanDocuments(new(_documents, "Projects", "axon"), CancellationToken.None);
    }

    [Fact] void should_refuse_the_excluded_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_remain_non_publishable() => _result.Artifacts.CanPublishArtifacts.ShouldBeFalse();
    [Fact] void should_render_nothing() => _result.Artifacts.Artifacts.ShouldBeEmpty();
    [Fact] void should_preserve_the_compiler_exclusions() => AssertStageExclusions(_result.Artifacts.Exclusions, _expected.Exclusions);
    [Fact] void should_explain_the_missing_facade_support() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == PackagedAxonRendering.ExclusionsNotSupportedCode && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error && diagnostic.Message.Contains("does not support compiler exclusions", StringComparison.Ordinal));
    [Fact] void should_not_misreport_a_renderer_bug() => _result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == AxonRenderingGuard.RendererFailureCode);
}
