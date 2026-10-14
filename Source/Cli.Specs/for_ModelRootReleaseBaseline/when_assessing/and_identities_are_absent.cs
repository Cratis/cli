// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_identities_are_absent : given.a_model_root
{
    void Establish() => File.Delete(_identitiesPath);
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_explain_the_required_identities() => _assessment.Reason!.ShouldContain(".screenplay/identities.json");
    [Fact] void should_not_run_the_persisted_check() => _run.DidNotReceive().Invoke(Arg.Any<string>());
    [Fact] void should_stamp_the_rendered_revision() => _assessment.ModelRevision.ShouldEqual(_compilation.Model.Revision);
}
