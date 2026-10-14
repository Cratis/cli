// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root.given;

public class a_persisted_model_root : for_ScreenplayReleaseBaseline.given.a_release_model
{
    protected readonly List<ArtifactRenderRequest> _requests = [];
    protected ISemanticModelCompiler _compiler = null!;
    private protected ScreenplayPlanning _planning = null!;
    private protected ScreenplayRenderPlan _result = null!;

    protected override string ModelSource => "concept RecordId : Uuid\ntype Details\n  value String\nmodule M\n  feature F\n    slice StateChange Record\n      command Record\n        recordId RecordId identifier\n        details Details\n        produces Recorded\n          for recordId\n          details = details\n      event Recorded\n        details Details\n    slice StateChange Observe\n      command Observe\n        recordId RecordId identifier\n        produces Observed\n          for recordId\n      event Observed\n";

    void Establish()
    {
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            var request = call.Arg<ArtifactRenderRequest>();
            _requests.Add(request);
            return new CratisArtifactRenderPlanner().Plan(request);
        });
        _compiler = Substitute.For<ISemanticModelCompiler>();
        _compiler.Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>()).Returns(call => new SemanticModelCompiler().Compile(call.Arg<string>(), call.Arg<SemanticDocumentSet>()));
        _planning = new(_compiler, new RenderTargetRoster([new CratisRenderTarget(planner)]));
    }

    protected ScreenplayWorkspace OpenWorkspace()
    {
        var outcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(_root);
        outcome.Success.ShouldBeTrue();
        outcome.Workspace!.Compilation.Success.ShouldBeTrue();
        return outcome.Workspace;
    }

    private protected ScreenplayDocumentRenderRequest TemporaryRequest()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var key = $"file-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("application.play")))}";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", File.ReadAllText(_document));
        return new(SemanticDocumentSet.Create([document], catalog), "Model", "cratis");
    }

    protected static SemanticEventContract Recorded(SemanticApplication application) => application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Record").Events.Single();
}
