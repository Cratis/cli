// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class without_persisted_identities : for_ScreenplayPlanning.given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    ScreenplayRenderPlan _legacy = null!;

    void Establish()
    {
        // Reproduce the unchanged temporary-identity request independently, without PATH/root discovery.
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var key = $"file-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("RegisterProject.play")))}";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "RegisterProject.play", Source);
        _legacy = new ScreenplayPlanning().PlanDocuments(new(SemanticDocumentSet.Create([document], catalog), "Projects", "cratis"), CancellationToken.None);
    }

    async Task Because() => _result = await Plan(_folder);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_compare_a_successful_legacy_plan() => _legacy.Success.ShouldBeTrue();
    [Fact] void should_compare_nonempty_artifacts() => _legacy.Artifacts!.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_keep_every_artifact_path_and_hash() => _result.Artifacts!.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)).ShouldEqual(_legacy.Artifacts!.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    [Fact] void should_keep_every_artifact_byte() => _result.Artifacts!.Artifacts.Zip(_legacy.Artifacts!.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_keep_the_semantic_revision() => _result.Artifacts!.SemanticRevision.ShouldEqual(_legacy.Artifacts!.SemanticRevision);
    [Fact] void should_not_create_metadata() => Directory.Exists(Path.Combine(_folder, ".screenplay")).ShouldBeFalse();
}
