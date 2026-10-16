// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_refused_model : given.a_serving_stage
{
    int _result;

    void Establish() => _statusBody = """{"state":"unsupported","engine":"eventmodel","issues":[{"capability":"Routing","artifact":"Register","details":"STAGE-ESM-030: Routes run only on the semantic engine."}]}""";

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Engine = "eventmodel" }, deadline.Token);
    }

    [Unix.Fact] void should_select_the_explicit_eventmodel_engine() => DockerArguments.ShouldContain("Stage__Runtime__Engine=eventmodel");
    [Unix.Fact] void should_start_only_one_container() => DockerArguments.Count(argument => argument == "run").ShouldEqual(1);
    [Unix.Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Unix.Fact] void should_stop_the_container_it_started() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
    [Unix.Fact] void should_stop_by_the_name_it_started() => DockerArguments[DockerArguments.ToList().IndexOf("stop") + 1].ShouldEqual(DockerArguments[3]);
    [Unix.Fact] void should_report_the_refusal() => _error.ToString().ShouldContain("STAGE-ESM-030");
    [Unix.Fact] void should_suggest_semantic() => _error.ToString().ShouldContain("--engine semantic");
}
