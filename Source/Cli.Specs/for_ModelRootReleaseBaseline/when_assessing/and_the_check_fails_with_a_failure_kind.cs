// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_the_check_fails_with_a_failure_kind : given.a_model_root
{
    void Establish() => _check = new(_root, _ => new ReleaseBaselineOutcome(1, [], "UnexpectedFailure", "something else went wrong"));
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_keep_the_reason() => _assessment.Reason.ShouldEqual("something else went wrong");
}
