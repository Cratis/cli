// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class an_output_json_document : given.a_release_target
{
    JsonElement _document;

    async Task Because()
    {
        await Check();
        var writer = new StringWriter();
        var previous = Console.Out;
        try
        {
            Console.SetOut(writer);
            ReleaseOutput.Write(OutputFormats.JsonCompact, _result);
        }
        finally
        {
            Console.SetOut(previous);
        }

        _document = JsonElement.Parse(writer.ToString());
    }

    [Fact] void should_carry_the_shared_schema() => _document.GetProperty("schema").GetString().ShouldEqual("cratis.compat/v1");
    [Fact] void should_carry_the_issue_format_field() => _document.GetProperty("format").GetString().ShouldEqual("cratis.compat/v1");
    [Fact] void should_carry_the_release_at_top_level_and_in_the_subject() => (_document.GetProperty("release").GetString() + _document.GetProperty("subject").GetProperty("release").GetString()).ShouldEqual("v3.2.0v3.2.0");
    [Fact] void should_carry_when_the_target_was_read() => _document.GetProperty("production").GetProperty("readAt").GetString()!.EndsWith('Z').ShouldBeTrue();
    [Fact] void should_carry_the_overall_verdict() => _document.GetProperty("verdict").GetString().ShouldEqual("compatible");
    [Fact] void should_carry_three_checks() => _document.GetProperty("checks").GetArrayLength().ShouldEqual(3);
    [Fact] void should_always_carry_the_action_as_a_string() => _document.GetProperty("checks")[0].GetProperty("action").GetString().ShouldEqual(string.Empty);
}
