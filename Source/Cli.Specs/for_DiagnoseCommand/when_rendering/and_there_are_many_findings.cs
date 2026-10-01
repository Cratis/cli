// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_there_are_many_findings : given.captured_reports
{
    void Establish() => _data = _data with
    {
        FailedPartitions = 20,
        Findings = [.. Enumerable.Range(1, 20).Select(x => new DiagnoseFinding("Failed partition", "store", "tenant-one", $"partition-{x}"))],
        ChecksCouldNotRun = [new DiagnoseCheckFailure("Observers", "store", "tenant-one", "Observer query failed")]
    };

    void Because() => CaptureReports();

    [Fact] void should_show_health_before_check_failures() => _outputs["watch"].IndexOf("Health", StringComparison.Ordinal).ShouldBeLessThan(_outputs["watch"].IndexOf("Could not check", StringComparison.Ordinal));
    [Fact] void should_show_check_failures_before_other_checks() => _outputs["watch"].IndexOf("Could not check", StringComparison.Ordinal).ShouldBeLessThan(_outputs["watch"].IndexOf("Connection", StringComparison.Ordinal));
    [Fact] void should_show_the_first_three_findings() => Enumerable.Range(1, 3).All(x => _outputs["watch"].Contains($"Failed partition: partition-{x}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_show_the_fourth_finding() => _outputs["watch"].ShouldNotContain("Failed partition: partition-4");
    [Fact] void should_direct_to_the_full_report() => _outputs["watch"].ShouldContain("+17 more → rerun without --watch for all findings");
    [Fact] void should_render_all_findings_in_text() => Enumerable.Range(1, 20).All(x => _outputs[OutputFormats.Table].Contains($"partition-{x}", StringComparison.Ordinal)).ShouldBeTrue();
}
