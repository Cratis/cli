// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_semantic_refusal : given.a_serving_stage
{
    int _result;

    void Establish() => _statusBody = """{"state":"unsupported","engine":"semantic","issues":[{"capability":"Reaction","artifact":"Notify","details":"STAGE-ESM-034: use the semantic path to preserve runsAs."}]}""";

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact }, deadline.Token);
    }

    [Unix.Fact] void should_select_semantic_by_default() => DockerArguments.ShouldContain("Stage__Runtime__Engine=semantic");
    [Unix.Fact] void should_start_only_one_container() => DockerArguments.Count(argument => argument == "run").ShouldEqual(1);
    [Unix.Fact] void should_stop_that_container() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
    [Unix.Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);

    [Unix.Fact]
    void should_report_one_refusal_without_a_retry_or_fallback()
    {
        using var json = JsonDocument.Parse(_error.ToString());
        json.RootElement.GetProperty("engine").GetString().ShouldEqual("semantic");
        json.RootElement.GetProperty("issues").GetArrayLength().ShouldEqual(1);
        json.RootElement.GetProperty("issues")[0].GetProperty("details").GetString()!.ShouldContain("STAGE-ESM-034");
        json.RootElement.GetProperty("suggestion").GetString()!.ShouldNotContain("--engine semantic");
        json.RootElement.TryGetProperty("engineFallback", out _).ShouldBeFalse();
    }
}
