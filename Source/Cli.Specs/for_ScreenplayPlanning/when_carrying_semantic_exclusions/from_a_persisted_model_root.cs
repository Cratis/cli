// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Fixture = Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions.given.an_excluded_slice;

namespace Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions;

public class from_a_persisted_model_root : for_ScreenplayReleaseBaseline.given.a_release_model
{
    readonly List<ArtifactRenderRequest> _requests = [];
    ScreenplayPlanning _planning = null!;
    ScreenplayRenderPlan _result = null!;

    protected override string ModelSource => Fixture.HealthySource;

    void Establish()
    {
        File.AppendAllText(_document, Fixture.RemovedSource);
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            var request = call.Arg<ArtifactRenderRequest>();
            _requests.Add(request);
            return new CratisArtifactRenderPlanner().Plan(request);
        });
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
    }

    async Task Because() => _result = await _planning.Plan(new(_root, "Model", "cratis"), CancellationToken.None);

    [Fact] void should_plan_the_admissible_workspace() => _result.Success.ShouldBeTrue();
    [Fact]
    void should_forward_the_authoritative_workspace_compiler_entries()
    {
        var opened = ScreenplayModelRoot.OpenForReleaseBaselineCheck(_root);
        opened.Success.ShouldBeTrue();
        var workspace = opened.Workspace;
        workspace.Compilation.Success.ShouldBeFalse();
        var documents = workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text));
        var expected = new SemanticModelCompiler().CompileAdmissible(workspace.ApplicationName, SemanticDocumentSet.Create([.. documents], workspace.IdentityCatalog, workspace.AttachmentContents));
        Fixture.AssertExclusions(_requests.Single().Exclusions, expected.Exclusions);
    }
}
