// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_findings_have_different_types : given.captured_reports
{
    void Establish() => _data = _data with
    {
        QuarantinedObservers = 1,
        FailedPartitions = 2,
        PendingRecommendations = 1,
        Findings =
        [
            new DiagnoseFinding("Quarantined observer", "store", "tenant-one", "quarantined-observer"),
            new DiagnoseFinding("Failed partition", "store", "tenant-one", "partition-one"),
            new DiagnoseFinding("Failed partition", "store", "tenant-one", "partition-two"),
            new DiagnoseFinding("Recommendation", "store", "tenant-one", "hidden-recommendation")
        ]
    };

    void Because() => CaptureReports();

    [Fact] void should_cap_findings_across_all_types() => _outputs["watch"].ShouldNotContain("hidden-recommendation");
    [Fact] void should_direct_to_a_report_containing_every_type() => _outputs["watch"].ShouldContain("+1 more → rerun without --watch for all findings");
    [Fact] void should_not_direct_only_to_failed_partitions() => _outputs["watch"].ShouldNotContain("failed-partitions list");
    [Fact] void should_include_the_hidden_recommendation_in_text() => _outputs[OutputFormats.Table].ShouldContain("hidden-recommendation");
    [Fact] void should_include_the_quarantined_observer_in_text() => _outputs[OutputFormats.Table].ShouldContain("quarantined-observer");
}
