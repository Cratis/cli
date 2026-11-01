// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand;

public class when_writing_json : Specification
{
    JsonElement _result;

    void Because() => _result = JsonSerializer.SerializeToElement(ReleaseNotesCommand.ToJson([new("3.87.0", null), new("3.86.0", "c")]), OutputFormatter.JsonSerializerOptions);

    [Fact] void should_list_every_release() => _result.GetArrayLength().ShouldEqual(2);
    [Fact] void should_carry_the_version() => _result[1].GetProperty("version").GetString().ShouldEqual("3.86.0");
    [Fact] void should_carry_the_notes() => _result[1].GetProperty("notes").GetString().ShouldEqual("c");
    [Fact] void should_carry_no_notes_for_a_release_this_build_lacks() => _result[0].TryGetProperty("notes", out _).ShouldBeFalse();
    [Fact] void should_carry_the_release_page() => _result[0].GetProperty("url").GetString().ShouldEqual("https://github.com/Cratis/cli/releases/tag/v3.87.0");
}
