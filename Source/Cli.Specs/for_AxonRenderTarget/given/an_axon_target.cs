// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Render.Axon;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_AxonRenderTarget.given;

public class an_axon_target : for_ScreenplayPlanning.given.a_screenplay_planning
{
    private protected IAxonRendering _rendering = null!;
    private protected AxonRenderTarget _target = null!;
    private protected SemanticCompilation _compilation = null!;
    private protected SemanticExecutionPlan _execution = null!;
    private protected ImmutableArray<SemanticImplementationRequirement> _requirements;
    private protected ImmutableArray<SemanticTypedContextDescriptor> _contexts;
    private protected AxonRenderInput _input = null!;
    private protected ArtifactRenderProfile _profile = null!;
    private protected ArtifactRenderPlan _plan = null!;
    private protected List<ScreenplayDiagnostic> _warnings = [];

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "Source.play", Source);
        var bound = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _compilation = bound.Value!;
        _requirements = bound.ImplementationRequirements;
        _contexts = bound.TypedContextDescriptors;
        _execution = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
        _profile = ArtifactRenderProfile.Create("axon", "0.0.0", "test", "0.0.0", []);
        var request = new ArtifactRenderRequest(_compilation.Model, _execution, _profile, new(ArtifactRenderScopeKind.Application, _compilation.Model.Application.Id));
        _plan = ArtifactRenderPlan.Create(request, [], [], [], [], []);
        _rendering = Substitute.For<IAxonRendering>();
        _rendering.Target.Returns("axon");
        _rendering.NewestAdmittedSemanticVersion.Returns(new SemanticVersion(34, 0));
        _rendering.Plan(Arg.Any<AxonRenderInput>()).Returns(call =>
        {
            _input = call.Arg<AxonRenderInput>();
            return _plan;
        });
        _target = new(_rendering);
    }
}
