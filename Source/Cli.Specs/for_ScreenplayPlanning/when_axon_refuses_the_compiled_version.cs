// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_axon_refuses_the_compiled_version : for_AxonRenderTarget.given.an_axon_target
{
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        _rendering.NewestAdmittedSemanticVersion.Returns(new SemanticVersion(0, 0));
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([_target]));
    }

    async Task Because() => _result = await Plan(target: "axon");

    [Fact] void should_block_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_target_admission() => _result.Diagnostics.Single(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Code.ShouldEqual(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_not_invoke_the_renderer() => _rendering.DidNotReceive().Plan(Arg.Any<AxonRenderInput>());
}
