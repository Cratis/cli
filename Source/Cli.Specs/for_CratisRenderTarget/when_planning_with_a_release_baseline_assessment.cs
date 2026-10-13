// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_CratisRenderTarget;

public class when_planning_with_a_release_baseline_assessment : given.a_cratis_render_target
{
    SemanticCompilation _compilation = null!;
    SemanticExecutionPlan _execution = null!;
    ArtifactRenderPlan _result = null!;
    ArtifactRenderPlan _withoutBaseline = null!;
    ArtifactRenderRequest _request = null!;
    ReleaseBaselineAssessment _assessment = null!;

    void Establish()
    {
        (_compilation, _execution) = Compile(SupportedSource);
        _assessment = new(ReleaseBaselineAssessmentStatus.Unavailable, _compilation.Model.Revision, [], "persisted identities required");
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            _request = call.Arg<ArtifactRenderRequest>();
            return new CratisArtifactRenderPlanner().Plan(_request);
        });
        _target = new(planner);
        _withoutBaseline = new CratisRenderTarget().Plan(_compilation, _execution, null, null, [], [], [], [], []);
    }

    void Because() => _result = _target.Plan(_compilation, _execution, null, null, [], [], [], [], [], releaseBaseline: _assessment);

    [Fact] void should_forward_the_assessment() => _request.ReleaseBaseline.ShouldBeSame(_assessment);
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_stage_unavailable_warning() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-004");
    [Fact] void should_keep_artifact_bytes() => _result.Artifacts.Zip(_withoutBaseline.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_keep_artifact_count() => _result.Artifacts.Length.ShouldEqual(_withoutBaseline.Artifacts.Length);
}
