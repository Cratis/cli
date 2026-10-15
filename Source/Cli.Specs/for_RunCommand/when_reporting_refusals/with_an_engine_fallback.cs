// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals;

[Collection(CliSpecsCollection.Name)]
public class with_an_engine_fallback : given.a_refusal_output
{
    const string Reason = "STAGE-ESM-034: use the semantic path to preserve runsAs.";

    [Theory]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Plain)]
    void should_announce_one_restart_line_for_humans(string format)
    {
        RunOutput.WriteEngineFallback(format, Reason);
        _output.ToString().ShouldContain("restarting from eventmodel to semantic");
        _output.ToString().ShouldContain(Reason);
        _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length.ShouldEqual(1);
    }

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    void should_emit_only_the_ready_document_with_fallback_details(string format)
    {
        RunOutput.WriteEngineFallback(format, Reason);
        RunOutput.WriteReady(format, new StageStartup("image"), "./models", StageEndpoints.For(9090, 35000), "semantic", Reason);
        using var json = JsonDocument.Parse(_output.ToString());
        var fallback = json.RootElement.GetProperty("engineFallback");
        fallback.GetProperty("from").GetString().ShouldEqual("eventmodel");
        fallback.GetProperty("to").GetString().ShouldEqual("semantic");
        fallback.GetProperty("reason").GetString().ShouldEqual(Reason);
    }

    [Theory]
    [InlineData(OutputFormats.Quiet)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_not_announce_a_switch_in_quiet_formats(string format)
    {
        RunOutput.WriteEngineFallback(format, Reason);
        _output.ToString().ShouldEqual(string.Empty);
        _error.ToString().ShouldEqual(string.Empty);
    }
}
