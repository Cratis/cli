// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_recovery_is_pending : given.a_model_root
{
    void Establish() => _run(_root).Returns(new ReleaseBaselineOutcome(2, [], "PendingOperation", "explicit recovery required"));
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_keep_the_reason() => _assessment.Reason.ShouldEqual("explicit recovery required");
}
