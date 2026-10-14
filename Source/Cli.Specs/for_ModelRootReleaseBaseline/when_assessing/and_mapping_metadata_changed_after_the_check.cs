// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_mapping_metadata_changed_after_the_check : given.a_model_root
{
    void Establish()
    {
        var entry = _baseline.Document!.Entries[0];
        _run(_root).Returns(new ReleaseBaselineOutcome(1, [new("BaselinedRevisionChanged", entry.Event, entry.Revision)]));
        File.WriteAllBytes(_baselinePath, ReleaseBaselineStore.Serialize(_baseline.Document with { Entries = [] }));
    }

    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_explain_the_mapping_drift() => _assessment.Reason!.ShouldContain("changed after its check");
    [Fact] void should_not_report_partial_violations() => _assessment.Violations.ShouldBeEmpty();
}
