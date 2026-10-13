// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_failed_partition_is_reported : given.captured_reports
{
    void Establish() => _data = new DiagnoseData("chronicle://chronicle-dev-client:secret@localhost:35100/", "Bookshop", "Default", true, "16.7.0", null, ["System", "Bookshop"], 9, 0, 0, 0, 1, 0, 22, DateTimeOffset.UtcNow)
    {
        TotalObservers = 9,
        Findings = [new DiagnoseFinding("Failed partition", "Bookshop", "Default", "BookInventory/9780134757599")]
    };

    void Because() => CaptureReports();

    [Fact] void should_render_aligned_rows_and_the_partition_finding() => _outputs[OutputFormats.Table].ShouldContain(string.Join(Environment.NewLine,
        "  ✓  Connection             connected",
        "  ✓  Server version         16.7.0",
        "  ✓  Event stores           2 stores: System, Bookshop",
        "  ✓  Observers              9 active  0 quarantined",
        "  ✓  Quarantined observers  0 quarantined (known count)",
        "  ✗  Failed partitions      1 need attention  → cratis chronicle failed-partitions list",
        "  ✓  Recommendations        none",
        "  ✓  Event sequence         tail: 22",
        "  !  Bookshop/Default: Failed partition: BookInventory/9780134757599"));
    [Fact] void should_end_the_text_report_with_the_community_pointer() => _outputs[OutputFormats.Table].TrimEnd().EndsWith("Questions? Ask on Discord: https://discord.gg/kt4AMpV8WV", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_add_the_community_pointer_to_machine_readable_reports() => new[] { OutputFormats.Json, OutputFormats.JsonCompact, OutputFormats.JsonQuiet, OutputFormats.Plain, OutputFormats.Quiet }.Any(format => _outputs[format].Contains("discord.gg", StringComparison.Ordinal)).ShouldBeFalse();
}
