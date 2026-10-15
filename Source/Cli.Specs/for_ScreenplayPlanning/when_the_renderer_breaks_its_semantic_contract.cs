// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_the_renderer_breaks_its_semantic_contract : Specification
{
    ScreenplayPlanning _planning = null!;
    ScreenplayDocumentRenderRequest _request = null!;
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        var corpus = RegisterProjectCorpus.LegacyV1;
        var form = corpus.SourceForms.Single(form => form.Name == "single");
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(corpus.ApplicationName));
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        _request = new(SemanticDocumentSet.Create([.. documents], catalog), corpus.ApplicationName, CratisRendering.TargetId, null, null);
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(_ => throw new InvalidSemanticContract("A generated concept rule escaped admission."));
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
    }

    void Because() => _result = _planning.PlanDocuments(_request, CancellationToken.None);

    [Fact] void should_refuse_the_render() => _result.Success.ShouldBeFalse();
    [Fact] void should_have_no_publishable_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_renderer_contract_failure() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "CLI-RENDER-005" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_preserve_the_contract_failure_message() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("A generated concept rule escaped admission.", StringComparison.Ordinal));
    [Fact] void should_direct_the_report_to_stage() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("Report this to Cratis/Stage", StringComparison.Ordinal));
}
