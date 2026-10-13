// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_the_baseline_is_unreadable : given.a_model_root
{
    void Establish() => _run(_root).Returns(new ReleaseBaselineOutcome(2, [], "UnreadableReleaseBaseline", "malformed metadata"));
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unreadable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unreadable);
    [Fact] void should_keep_the_reason() => _assessment.Reason.ShouldEqual("malformed metadata");
}
