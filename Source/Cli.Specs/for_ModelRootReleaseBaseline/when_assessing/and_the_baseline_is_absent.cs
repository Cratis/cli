// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_the_baseline_is_absent : given.a_model_root
{
    void Establish() => File.Delete(_baselinePath);
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_not_supply_an_assessment() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.NotSupplied);
    [Fact] void should_not_run_the_persisted_check() => _run.DidNotReceive().Invoke(Arg.Any<string>());
}
