// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_mapping_metadata_is_unreadable : given.a_model_root
{
    void Establish()
    {
        var entry = _baseline.Document!.Entries[0];
        _run(_root).Returns(new ReleaseBaselineOutcome(1, [new("BaselinedRevisionChanged", entry.Event, entry.Revision)]));
        File.WriteAllText(_baselinePath, "malformed metadata");
    }

    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_unreadable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unreadable);
    [Fact] void should_not_report_partial_violations() => _assessment.Violations.ShouldBeEmpty();
}
