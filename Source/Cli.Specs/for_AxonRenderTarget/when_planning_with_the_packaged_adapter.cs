// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Axon;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Axon;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_planning_with_the_packaged_adapter : given.an_axon_target
{
    ArtifactRenderPlan _actual = null!;
    ArtifactRenderPlan _expected = null!;

    void Establish()
    {
        var selection = RenderSelection.Resolve(["Projects.Registration.RegisterProject", "Projects.Registration.ProjectLookup"], SemanticAddressIndex.From(_compilation.Model.Application, true), []);
        _input = new(_compilation.Model, _execution, selection.Scopes[0])
        {
            ProjectName = "projects-api",
            JavaPackage = "com.example.projects",
            AdditionalScopes = selection.Scopes.RemoveAt(0),
            ImplementationRequirements = _requirements,
            TypedContextDescriptors = _contexts,
            ReleaseBaseline = ReleaseBaselineAssessment.NotSupplied,
            FileHeader = ["Example header"],
            Strict = false
        };
        _expected = AxonRendering.Plan(new(_input.Model, _input.ExecutionPlan, _input.Scope)
        {
            AdditionalScopes = _input.AdditionalScopes,
            ImplementationRequirements = _input.ImplementationRequirements,
            ImplementationContents = _input.ImplementationContents,
            TypedContextDescriptors = _input.TypedContextDescriptors,
            AttachmentDiagnostics = _input.AttachmentDiagnostics,
            ReleaseBaseline = _input.ReleaseBaseline,
            Options = new() { ProjectName = _input.ProjectName, JavaPackage = _input.JavaPackage, FileHeader = _input.FileHeader, Strict = _input.Strict }
        });
    }

    void Because() => _actual = new PackagedAxonRendering().Plan(_input);

    [Fact] void should_render_artifacts() => _actual.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_keep_the_facade_output_and_diagnostics() => _actual.Digest.ShouldEqual(_expected.Digest);
    [Fact] void should_keep_the_semantic_revision() => _actual.SemanticRevision.ShouldEqual(_expected.SemanticRevision);
    [Fact] void should_keep_the_primary_scope() => _actual.Scope.ShouldEqual(_input.Scope);
    [Fact] void should_keep_additional_scopes() => _actual.AdditionalScopes.ShouldEqual(_input.AdditionalScopes);
    [Fact] void should_keep_the_renderer_identity() => _actual.RendererVersion.ShouldEqual(_expected.RendererVersion);
}
