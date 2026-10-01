// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_checks_could_not_run : given.captured_reports
{
    void Establish()
    {
        _data = new DiagnoseData("chronicle://user:secret@localhost:35000", "store", "tenant-one", true, "19.6.1", null, ["store"], 1, 2, 3, 4, 0, 0, null, DateTimeOffset.UtcNow)
        {
            QuarantinedObservers = 1,
            ChecksCouldNotRun = [new DiagnoseCheckFailure("Failed partitions", "store", "tenant-one", "Permission denied [scope]")],
            Findings = [new DiagnoseFinding("Quarantined observer", "store", "tenant-one", "observer")]
        };
        _data = _data with { Scopes = [_data] };
    }

    void Because() => CaptureReports();

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    [InlineData(OutputFormats.Plain)]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Auto)]
    [InlineData(OutputFormats.Quiet)]
    [InlineData("watch")]
    void should_show_the_reason_in_every_format(string format) => _outputs[format].ShouldContain("Permission denied [scope]");
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    [InlineData(OutputFormats.Plain)]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Auto)]
    [InlineData(OutputFormats.Quiet)]
    [InlineData("watch")]
    void should_name_the_namespace_in_every_format(string format) => _outputs[format].ShouldContain("tenant-one");
    [Fact] void should_count_quarantine_in_plain_output() => _outputs[OutputFormats.Plain].ShouldContain("observers_quarantined=1");
    [Fact] void should_distinguish_incomplete_checks_in_plain_output() => _outputs[OutputFormats.Plain].ShouldContain("checks_complete=False");
    [Fact] void should_distinguish_incomplete_checks_in_text_output() => _outputs[OutputFormats.Table].ShouldContain("Could not check:");
    [Fact] void should_distinguish_incomplete_checks_in_watch_output() => _outputs["watch"].ShouldContain("Could not check");
    [Fact] void should_not_show_a_passed_partition_check_in_text() => _outputs[OutputFormats.Table].ShouldContain("could not check (see reasons below)");
    [Fact] void should_point_to_the_reasons_above_in_watch() => _outputs["watch"].ShouldContain("could not check (see reasons above)");
    [Fact] void should_show_quarantine_in_text() => _outputs[OutputFormats.Table].ShouldContain("1 quarantined");
    [Fact] void should_show_quarantine_in_watch() => _outputs["watch"].ShouldContain("1 quarantined");
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    [InlineData(OutputFormats.Plain)]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Auto)]
    [InlineData(OutputFormats.Quiet)]
    [InlineData("watch")]
    void should_not_leak_connection_credentials(string format) => _outputs[format].ShouldNotContain("secret");
    [Fact] void should_report_unhealthy_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("healthy").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_incomplete_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("checksComplete").GetBoolean().ShouldBeFalse();
    [Fact] void should_separate_unavailable_checks_from_findings_in_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("checksCouldNotRun").GetArrayLength().ShouldEqual(1);
    [Fact] void should_include_all_states_in_the_json_total() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("observers").GetProperty("total").GetInt32().ShouldEqual(11);
    [Fact] void should_include_all_states_in_the_scope_json_total() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("scopes")[0].GetProperty("observers").GetProperty("total").GetInt32().ShouldEqual(11);
    [Fact] void should_count_quarantine_in_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("observers").GetProperty("quarantined").GetInt32().ShouldEqual(1);
}
