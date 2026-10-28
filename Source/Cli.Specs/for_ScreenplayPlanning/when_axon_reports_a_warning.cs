// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_axon_reports_a_warning : for_AxonRenderTarget.given.an_axon_target
{
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([_target]));
        var request = new ArtifactRenderRequest(_compilation.Model, _execution, _profile, new(ArtifactRenderScopeKind.Application, _compilation.Model.Application.Id));
        _plan = ArtifactRenderPlan.Create(request, [], [new("STAGE-AXON-TEST", ArtifactRenderDiagnosticSeverity.Warning, "Unsupported intent remains visible.", _compilation.Model.Application.Id)], [], [], []);
        _rendering.Plan(Arg.Any<AxonRenderInput>()).Returns(_plan);
    }

    async Task Because() => _result = await Plan(target: "axon");

    [Fact] void should_keep_the_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-AXON-TEST").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_keep_the_message() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-AXON-TEST").Message.ShouldEqual("Unsupported intent remains visible.");
    [Fact] void should_keep_the_renderers_plan() => _result.Artifacts.ShouldEqual(_plan);
    [Fact] void should_carry_the_target_into_publication() => _result.Target.ShouldEqual(_target);
    [Fact] void should_not_prepare_cratis_scaffold_options() => _result.ScaffoldOptions.ShouldBeNull();
}
