// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_authored_document_set : given.a_canonical_screenplay
{
    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    public void should_preserve_the_corpus_catalog_and_render_the_exact_frozen_semantics(string formName)
    {
        var form = Corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var sources = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var documents = SemanticDocumentSet.Create([.. sources], catalog);
        var expectedModel = SemanticModelSerializer.Deserialize(Corpus.EsmBytes.AsSpan());
        var expectedExecution = SemanticExecutionPlan.Compile(expectedModel);
        expectedExecution.Success.ShouldBeTrue();
        var options = new CratisRenderingOptions("Delivery.Backend", "Company.Projects");
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, expectedModel.Application.Id);
        var expected = CratisRendering.Plan(expectedModel, expectedExecution.Plan!, scope, options);
        expected.Success.ShouldBeTrue();
        var request = new ScreenplayDocumentRenderRequest(documents, Corpus.ApplicationName, CratisRendering.TargetId, options.ProjectName, options.RootNamespace);

        var result = _planning.PlanDocuments(request, CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Documents.ShouldEqual(documents.Documents.Length);
        SemanticModelSerializer.Serialize(_requests.Single().Model).SequenceEqual(Corpus.EsmBytes).ShouldBeTrue();
        when_planning_canonical_source_forms.AssertPlan(result.Artifacts!, expected);
    }

    [Fact]
    public void should_preserve_explicitly_assigned_command_and_document_identities()
    {
        var form = Corpus.SourceForms.Single(form => form.Name == "single");
        var original = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var command = SemanticId.Create(SemanticKind.Command, "authored-register-project");
        var documentId = DocumentId.Create("persisted-document-identity");
        var assignments = original.Semantics.Select(assignment => assignment.Address.Kind == SemanticKind.Command
            ? assignment with { Id = command, Origin = SemanticIdentityOrigin.Persisted }
            : assignment);
        var catalog = SemanticIdentityCatalog.Create(
            original.Application,
            [new DocumentIdentityAssignment("logical-input", documentId, SemanticIdentityOrigin.Persisted)],
            [.. assignments],
            original.EventContracts);
        var documents = SemanticDocumentSet.Create(
            [SemanticSourceDocument.Create(documentId, "logical-input", "elsewhere/renamed.play", form.Documents.Single().Text)], catalog);

        var result = _planning.PlanDocuments(new ScreenplayDocumentRenderRequest(documents, Corpus.ApplicationName, CratisRendering.TargetId), CancellationToken.None);

        result.Success.ShouldBeTrue();
        var compiledInput = _documentSets.Single();
        compiledInput.Documents.Single().Id.ShouldEqual(documentId);
        compiledInput.Documents.Single().StableKey.ShouldEqual("logical-input");
        compiledInput.Documents.Single().DisplayPath.ShouldEqual("elsewhere/renamed.play");
        compiledInput.Documents.Single().Text.ShouldEqual(form.Documents.Single().Text);
        SemanticIdentityCatalogSerializer.Serialize(compiledInput.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(catalog)).ShouldBeTrue();
        _requests.Single().Model.Application.Modules.Single().Features.Single().Slices
            .Single(slice => slice.Kind == SemanticSliceKind.StateChange).Commands.Single().Id.ShouldEqual(command);
        _requests.Single().Model.Revision.ShouldNotEqual(Corpus.SemanticRevision);
    }

    [Fact]
    public void should_not_begin_planning_a_canceled_document_request()
    {
        var form = Corpus.SourceForms.Single(form => form.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var document = form.Documents.Single();
        var documents = SemanticDocumentSet.Create(
            [SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text)], catalog);

        var failure = Catch.Exception(() => _planning.PlanDocuments(new ScreenplayDocumentRenderRequest(documents, Corpus.ApplicationName, CratisRendering.TargetId), new CancellationToken(canceled: true)));

        failure.ShouldBeOfExactType<OperationCanceledException>();
        _documentSets.ShouldBeEmpty();
        _requests.ShouldBeEmpty();
    }
}
