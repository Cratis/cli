// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_the_check_is_clean : given.a_model_root
{
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_check_the_model_root() => _run.Received(1).Invoke(_root);
    [Fact] void should_report_checked() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_have_no_violations() => _assessment.Violations.ShouldBeEmpty();
    [Fact] void should_stamp_the_rendered_revision() => _assessment.ModelRevision.ShouldEqual(_compilation.Model.Revision);
}
