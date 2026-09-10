// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_CratisRenderTarget;

public class when_planning_unsupported_semantics : given.a_cratis_render_target
{
    ExecutableSemanticModel _model = null!;
    SemanticExecutionPlan _executionPlan = null!;
    ArtifactRenderPlan _result = null!;

    void Establish() => (_model, _executionPlan) = Compile(UnsupportedSource);

    Task Because()
    {
        _result = _target.Plan(_model, _executionPlan);
        return Task.CompletedTask;
    }

    [Fact] void should_not_be_successful() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_facade_blocking_diagnostic() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-006");
    [Fact] void should_plan_zero_artifacts() => _result.Artifacts.Length.ShouldEqual(0);
}
