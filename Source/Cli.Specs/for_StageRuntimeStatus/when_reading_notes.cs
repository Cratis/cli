// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRuntimeStatus;

public class when_reading_notes : Specification
{
    [Theory]
    [InlineData("ready")]
    [InlineData("unsupported")]
    void should_preserve_notes_without_changing_admission(string state)
    {
        var result = StageRuntimeStatus.Read($$"""{"state":"{{state}}","engine":"semantic","notes":[{"code":"PLAY0270","message":"Persona 'Reader' is authoring metadata.","source":"model.play(2,1)","severity":"Information"}]}""")!;
        result.State.ShouldEqual(state);
        result.Notes!.Single().Code.ShouldEqual("PLAY0270");
        result.Notes!.Single().Message.ShouldEqual("Persona 'Reader' is authoring metadata.");
        result.Notes!.Single().Source.ShouldEqual("model.play(2,1)");
    }

    [Theory]
    [InlineData("\"Information\"")]
    [InlineData("\"Warning\"")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    [InlineData("\"FutureSeverity\"")]
    [InlineData("null")]
    void should_accept_and_retain_string_numeric_and_unknown_severity(string severity)
    {
        var result = StageRuntimeStatus.Read($$"""{"state":"ready","notes":[{"code":"PLAY0270","message":"Ignored annotation.","severity":{{severity}}}]}""")!;
        (result.Notes!.Single().Severity?.GetRawText()).ShouldEqual(string.Equals(severity, "null", StringComparison.Ordinal) ? null : severity);
    }

    [Theory]
    [InlineData("{\"state\":\"ready\"}")]
    [InlineData("{\"state\":\"ready\",\"notes\":null}")]
    void should_accept_older_images_without_notes(string body) => StageRuntimeStatus.Read(body)!.Notes.ShouldBeNull();

    [Fact]
    void should_accept_a_note_without_optional_fields()
    {
        var note = StageRuntimeStatus.Read("""{"state":"ready","notes":[{"code":"PLAY0270","message":"Ignored annotation."}]}""")!.Notes!.Single();
        note.Source.ShouldBeNull();
        note.Severity.ShouldBeNull();
    }
}
