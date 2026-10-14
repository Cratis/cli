// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageOutput;

[Collection(CliSpecsCollection.Name)]
public class when_reporting_a_refusal : Specification
{
    TextWriter _previousError;
    StringWriter _error;

    void Establish()
    {
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Because() => RunOutput.WriteRefusal(OutputFormats.JsonCompact, new("unsupported", "eventmodel", [
        new("Routing", "Register", "STAGE-ESM-030: command routes"),
        new("Capture", "Import", "foreign events")
    ]));

    [Fact] void should_report_the_routing_issue() => _error.ToString().ShouldContain("Unsupported(Routing) Register: STAGE-ESM-030: command routes");
    [Fact] void should_report_the_capture_issue() => _error.ToString().ShouldContain("Unsupported(Capture) Import: foreign events");
    [Fact] void should_name_the_actual_engine() => _error.ToString().ShouldContain("The model cannot run on the eventmodel engine");
    [Fact] void should_suggest_the_semantic_engine() => _error.ToString().ShouldContain("--engine semantic");

    void Destroy()
    {
        Console.SetError(_previousError);
        _error.Dispose();
    }
}
