// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_reporting_notes;

[Collection(CliSpecsCollection.Name)]
public class with_a_refusal : given.a_notes_output
{
    [Theory]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Plain)]
    void should_group_notes_as_ignored_without_hiding_the_refusal(string format)
    {
        RunOutput.WriteRefusal(format, _status);
        _output.ToString().ShouldContain("Ignored by this engine (2)");
        _output.ToString().ShouldContain("2 × PLAY0270: Concept '…' declares a report-only annotation. at one.play(1,1), two.play(2,1)");
        _output.ToString().ShouldContain("Not supported by this engine yet (1)");
        $"{_output}{_error}".ShouldContain("The model cannot run on the semantic engine");
    }

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    void should_preserve_raw_notes_issues_and_groups_in_one_refusal_document(string format)
    {
        RunOutput.WriteRefusal(format, _status);
        using var json = JsonDocument.Parse(_error.ToString());
        var result = json.RootElement;
        result.GetProperty("error").GetString().ShouldEqual(ExitCodes.ValidationErrorCode);
        result.GetProperty("issues").GetArrayLength().ShouldEqual(1);
        var notes = result.GetProperty("notes");
        notes.GetArrayLength().ShouldEqual(2);
        notes[0].GetProperty("message").GetString().ShouldEqual(Notes[0].Message);
        notes[0].GetProperty("source").GetString().ShouldEqual("one.play(1,1)");
        notes[0].GetProperty("severity").GetString().ShouldEqual("Information");
        notes[1].GetProperty("severity").GetInt32().ShouldEqual(1);
        result.GetProperty("groups")[1].GetProperty("category").GetString().ShouldEqual(StageRefusalGroups.Ignored);
        result.GetProperty("groups")[1].GetProperty("count").GetInt32().ShouldEqual(2);
        _output.ToString().ShouldEqual(string.Empty);
    }
}
