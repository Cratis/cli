// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_StageOutput;

[Collection(CliSpecsCollection.Name)]
public class when_reporting_readiness : Specification
{
    TextWriter _previousOutput;
    StringWriter _output;

    void Establish()
    {
        _previousOutput = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    [InlineData(null)]
    void should_include_only_a_reported_engine_in_json(string? engine)
    {
        RunOutput.WriteReady(OutputFormats.JsonCompact, new StageStartup("image"), "/models", StageEndpoints.For(9090, 35000), engine);
        using var json = JsonDocument.Parse(_output.ToString());
        json.RootElement.TryGetProperty("engine", out var value).ShouldEqual(engine is not null);
        if (engine is not null) value.GetString().ShouldEqual(engine);
    }

    [Fact]
    void should_name_the_reported_engine_in_plain_output()
    {
        RunOutput.WriteReady(OutputFormats.Plain, new StageStartup("image"), "/models", StageEndpoints.For(9090, 35000), "semantic");
        _output.ToString().ShouldContain("engine\tsemantic");
    }

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        _output.Dispose();
    }
}
