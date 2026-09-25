// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_a_workspace_document_set : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        // An existing sibling attachment must not be loaded by PlanDocuments.
        WriteAttachment("return true;");
    }

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("orders"), "orders", "Orders.play", FileSource);
        _result = _planning.PlanDocuments(new(SemanticDocumentSet.Create([document], catalog), "Orders", "cratis"), CancellationToken.None);
    }

    [Fact] void should_not_read_the_sibling_file() => _supplied!.AttachmentContents.ShouldBeEmpty();
    [Fact] void should_pass_the_compilers_requirement_to_stage() => _renderRequest!.ImplementationRequirements.Single().RequirementId.ShouldEqual(_compiled!.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_not_supply_a_file_body() => _renderRequest!.ImplementationContents.ShouldBeEmpty();
    [Fact] void should_block_the_unresolved_file_reference() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-020");
    [Fact] void should_not_publish_artifacts() => _result.Success.ShouldBeFalse();
}
