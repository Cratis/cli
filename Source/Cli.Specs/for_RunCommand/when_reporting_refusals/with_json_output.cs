// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals;

[Collection(CliSpecsCollection.Name)]
public class with_json_output : given.a_refusal_output
{
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    void should_keep_all_issues_and_groups_in_one_document(string format)
    {
        RunOutput.WriteRefusal(format, _status, fallbackReason: "Use the semantic path to preserve runsAs.");
        using var json = JsonDocument.Parse(_error.ToString());
        var result = json.RootElement;
        result.GetProperty("error").GetString().ShouldEqual(ExitCodes.ValidationErrorCode);
        result.GetProperty("issues").GetArrayLength().ShouldEqual(7);
        result.GetProperty("issues")[4].GetProperty("details").GetString().ShouldEqual(_status.Issues![4].Details);
        var groups = result.GetProperty("groups");
        groups.GetArrayLength().ShouldEqual(3);
        groups[0].GetProperty("category").GetString().ShouldEqual("Fix your model");
        groups[2].GetProperty("kind").GetString().ShouldEqual("Persona '…' is authoring metadata.");
        groups[2].GetProperty("count").GetInt32().ShouldEqual(5);
        groups[2].GetProperty("locations").GetArrayLength().ShouldEqual(3);
        result.GetProperty("engineFallback").GetProperty("reason").GetString().ShouldEqual("Use the semantic path to preserve runsAs.");
        _output.ToString().ShouldEqual(string.Empty);
    }

    [Fact]
    void should_include_the_exact_retry_command_in_the_refusal_document()
    {
        RunOutput.WriteRefusal(OutputFormats.JsonCompact, new("unsupported", "eventmodel", [new("Routing", "Register", "Routes run only on the semantic engine.")]), new() { Path = "./my models", Port = 9191 });
        using var json = JsonDocument.Parse(_error.ToString());
        json.RootElement.GetProperty("suggestion").GetString().ShouldEqual("Run again on the semantic engine: cratis run \"./my models\" --engine semantic --port 9191");
    }
}
