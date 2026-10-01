// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Spectre.Console;

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_checks_could_not_run : Specification
{
    DiagnoseData _data;
    readonly Dictionary<string, string> _outputs = new(StringComparer.Ordinal);

    void Establish() => _data = new DiagnoseData("chronicle://user:secret@localhost:35000", "store", "tenant-one", true, "19.6.1", null, ["store"], 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow)
    {
        QuarantinedObservers = 1,
        ChecksCouldNotRun = [new DiagnoseCheckFailure("Failed partitions", "store", "tenant-one", "Permission denied [scope]")],
        Findings = [new DiagnoseFinding("Quarantined observer", "store", "tenant-one", "observer")]
    };

    void Because()
    {
        foreach (var format in new[] { OutputFormats.Json, OutputFormats.JsonCompact, OutputFormats.Plain, OutputFormats.Table, "watch" })
        {
            using var writer = new StringWriter();
            var previousOutput = Console.Out;
            var previousConsole = AnsiConsole.Console;
            try
            {
                Console.SetOut(writer);
                AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
                {
                    Out = new AnsiConsoleOutput(writer),
                    Ansi = AnsiSupport.No
                });
                AnsiConsole.Console.Profile.Width = 240;
                if (format == "watch")
                {
                    AnsiConsole.Write(DiagnoseCommand.BuildWatchReport(_data));
                }
                else
                {
                    DiagnoseCommand.Render(format, _data);
                }

                _outputs[format] = writer.ToString();
            }
            finally
            {
                Console.SetOut(previousOutput);
                AnsiConsole.Console = previousConsole;
            }
        }
    }

    [Fact] void should_show_the_reason_in_every_format() => _outputs.Values.All(x => x.Contains("Permission denied [scope]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_namespace_in_every_format() => _outputs.Values.All(x => x.Contains("tenant-one", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_count_quarantine_in_plain_output() => _outputs[OutputFormats.Plain].ShouldContain("observers_quarantined=1");
    [Fact] void should_distinguish_incomplete_checks_in_plain_output() => _outputs[OutputFormats.Plain].ShouldContain("checks_complete=False");
    [Fact] void should_distinguish_incomplete_checks_in_text_output() => _outputs[OutputFormats.Table].ShouldContain("Could not check:");
    [Fact] void should_distinguish_incomplete_checks_in_watch_output() => _outputs["watch"].ShouldContain("Could not check");
    [Fact] void should_not_show_a_passed_partition_check_in_text() => _outputs[OutputFormats.Table].ShouldContain("could not check (see reasons below)");
    [Fact] void should_show_quarantine_in_text() => _outputs[OutputFormats.Table].ShouldContain("1 quarantined");
    [Fact] void should_show_quarantine_in_watch() => _outputs["watch"].ShouldContain("1 quarantined");
    [Fact] void should_not_leak_connection_credentials() => _outputs.Values.All(x => !x.Contains("secret", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_unhealthy_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("healthy").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_incomplete_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("checksComplete").GetBoolean().ShouldBeFalse();
    [Fact] void should_separate_unavailable_checks_from_findings_in_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("checksCouldNotRun").GetArrayLength().ShouldEqual(1);
    [Fact] void should_count_quarantine_in_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("observers").GetProperty("quarantined").GetInt32().ShouldEqual(1);
}
