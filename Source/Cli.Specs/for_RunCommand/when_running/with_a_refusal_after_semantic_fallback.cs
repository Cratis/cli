// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_refusal_after_semantic_fallback : given.a_fallback_stage
{
    int _result;

    void Establish() => _secondStatus = """{"state":"unsupported","engine":"semantic","issues":[{"capability":"World","artifact":"History","details":"History cannot be reconstructed."}]}""";

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact }, deadline.Token);
        _arguments = await File.ReadAllLinesAsync(_dockerArguments);
    }

    [Unix.Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Unix.Fact] void should_switch_only_once() => _arguments.Count(argument => argument == "run").ShouldEqual(2);
    [Unix.Fact] void should_stop_both_containers() => _arguments.Count(argument => argument == "stop").ShouldEqual(2);
    [Unix.Fact] void should_not_emit_a_separate_fallback_document() => _output.ToString().ShouldEqual(string.Empty);

    [Unix.Fact]
    void should_report_only_semantic_issues_and_retain_why_it_switched()
    {
        using var json = JsonDocument.Parse(_error.ToString());
        json.RootElement.GetProperty("engine").GetString().ShouldEqual("semantic");
        var issues = json.RootElement.GetProperty("issues");
        issues.GetArrayLength().ShouldEqual(1);
        issues[0].GetProperty("details").GetString().ShouldEqual("History cannot be reconstructed.");
        json.RootElement.GetProperty("engineFallback").GetProperty("reason").GetString()!.ShouldContain("STAGE-ESM-034");
    }
}
