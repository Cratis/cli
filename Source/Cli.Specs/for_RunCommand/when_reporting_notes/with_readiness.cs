// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_reporting_notes;

[Collection(CliSpecsCollection.Name)]
public class with_readiness : given.a_notes_output
{
    [Fact]
    void should_show_only_a_muted_count_in_table_output()
    {
        RunOutput.WriteReady(OutputFormats.Table, new StageStartup("image"), "./models", StageEndpoints.For(9090, 35000), notes: Notes);
        _output.ToString().ShouldContain("2 notes the engine ignored (use --verbose to list them)");
        _output.ToString().ShouldNotContain("PLAY0270");
        _output.ToString().ShouldContain("Ready");
    }

    [Theory]
    [InlineData(OutputFormats.Table, true)]
    [InlineData(OutputFormats.Plain, false)]
    void should_list_one_line_per_note_group(string format, bool verbose)
    {
        RunOutput.WriteReady(format, new StageStartup("image"), "./models", StageEndpoints.For(9090, 35000), notes: Notes, verbose: verbose);
        _output.ToString().ShouldContain("Ignored by this engine (2)");
        _output.ToString().ShouldContain("2 × PLAY0270: Concept '…' declares a report-only annotation. at one.play(1,1), two.play(2,1)");
        _output.ToString().ShouldNotContain("use --verbose");
    }

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    void should_include_raw_notes_in_the_ready_document(string format)
    {
        RunOutput.WriteReady(format, new StageStartup("image"), "./models", StageEndpoints.For(9090, 35000), notes: Notes);
        using var json = JsonDocument.Parse(_output.ToString());
        json.RootElement.GetProperty("status").GetString().ShouldEqual("ready");
        json.RootElement.TryGetProperty("engineFallback", out _).ShouldBeFalse();
        var notes = json.RootElement.GetProperty("notes");
        notes.GetArrayLength().ShouldEqual(2);
        notes[0].GetProperty("code").GetString().ShouldEqual("PLAY0270");
        notes[0].GetProperty("message").GetString().ShouldEqual(Notes[0].Message);
        notes[0].GetProperty("source").GetString().ShouldEqual("one.play(1,1)");
        notes[0].GetProperty("severity").GetString().ShouldEqual("Information");
        notes[1].GetProperty("severity").GetInt32().ShouldEqual(1);
        _error.ToString().ShouldEqual(string.Empty);
    }

    [Fact]
    void should_not_show_a_note_summary_for_an_older_image()
    {
        RunOutput.WriteReady(OutputFormats.Table, new StageStartup("image"), "./models", StageEndpoints.For(9090, 35000));
        _output.ToString().ShouldNotContain("notes");
    }

    [Theory]
    [InlineData(OutputFormats.Quiet)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_not_show_ready_notes_in_quiet_output(string format)
    {
        RunOutput.WriteReady(format, new StageStartup("image"), "./models", StageEndpoints.For(9090, 35000), notes: Notes, verbose: true);
        _output.ToString().ShouldEqual(string.Empty);
        _error.ToString().ShouldEqual(string.Empty);
    }
}
