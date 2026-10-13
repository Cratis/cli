// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.when_assessing;

public class and_the_check_reports_violations : given.a_model_root
{
    void Establish() => _run(_root).Returns(new ReleaseBaselineOutcome(1, [.. _baseline.Document!.Entries.Select((entry, index) => new ReleaseBaselineViolation(index == 0 ? "BaselinedRevisionChanged" : "BaselinedRevisionMissing", entry.Event, entry.Revision, index == 0 ? "properties[0].type" : null))]));
    void Because() => _assessment = _check.Assess(_compilation);

    [Fact] void should_report_checked() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_map_every_admitted_revision() => _assessment.Violations.Select(violation => (violation.ContractId, violation.Revision, violation.StoredName, violation.AdmittedIn)).ShouldEqual(_baseline.Document!.Entries.Select(entry => (EventContractId.Parse(entry.Event), entry.Revision, entry.EventType, entry.AdmittedIn)));
    [Fact] void should_map_display_hint_kinds() => _assessment.Violations.Select(violation => violation.DisplayHint.Kind).ShouldEqual(_baseline.Document!.Entries.Select(entry => entry.LastAddress.Kind));
    [Fact] void should_map_display_hint_parts() => _assessment.Violations.SelectMany(violation => violation.DisplayHint.Parts).ShouldEqual(_baseline.Document!.Entries.SelectMany(entry => entry.LastAddress.Parts));
    [Fact] void should_keep_violation_kinds() => _assessment.Violations.Select(violation => violation.Kind).ShouldEqual(["BaselinedRevisionChanged", "BaselinedRevisionMissing"]);
    [Fact] void should_keep_the_difference_path() => _assessment.Violations[0].Path.ShouldEqual("properties[0].type");
    [Fact] void should_stamp_the_rendered_revision() => _assessment.ModelRevision.ShouldEqual(_compilation.Model.Revision);
}
