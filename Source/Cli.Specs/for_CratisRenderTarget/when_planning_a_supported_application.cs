// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_CratisRenderTarget;

public class when_planning_a_supported_application : given.a_cratis_render_target
{
    ExecutableSemanticModel _model = null!;
    SemanticExecutionPlan _executionPlan = null!;
    ArtifactRenderPlan _result = null!;
    ArtifactRenderPlan _expected = null!;

    void Establish() => (_model, _executionPlan) = Compile(SupportedSource);

    Task Because()
    {
        _result = _target.Plan(_model, _executionPlan);
        _expected = PlanWithFacade(_model, _executionPlan);
        return Task.CompletedTask;
    }

    [Fact] void should_expose_the_published_facade_target_id_as_its_name() => _target.Name.ShouldEqual(CratisRendering.TargetId);
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_plan_the_exact_facade_target() => _result.Target.ShouldEqual(_expected.Target);
    [Fact] void should_plan_the_exact_facade_target_version() => _result.TargetVersion.ShouldEqual(_expected.TargetVersion);
    [Fact] void should_plan_the_exact_facade_renderer() => _result.Renderer.ShouldEqual(_expected.Renderer);
    [Fact] void should_plan_the_exact_facade_renderer_version() => _result.RendererVersion.ShouldEqual(_expected.RendererVersion);
    [Fact] void should_plan_the_same_application_name() => _result.ApplicationName.ShouldEqual(_expected.ApplicationName);
    [Fact] void should_plan_the_same_semantic_revision() => _result.SemanticRevision.ShouldEqual(_expected.SemanticRevision);
    [Fact] void should_plan_the_exact_facade_artifacts() =>
        _result.Artifacts.Select(_ => (_.Kind, _.RelativePath, _.Sha256))
            .ShouldEqual(_expected.Artifacts.Select(_ => (_.Kind, _.RelativePath, _.Sha256)));
    [Fact] void should_plan_the_exact_facade_diagnostics() =>
        _result.Diagnostics.Select(_ => (_.Code, _.Severity, _.Message))
            .ShouldEqual(_expected.Diagnostics.Select(_ => (_.Code, _.Severity, _.Message)));
    [Fact] void should_plan_the_full_published_facade_scaffold_and_nothing_the_cli_authors_itself() =>
        _result.Artifacts.Length.ShouldEqual(_expected.Artifacts.Length);
}
