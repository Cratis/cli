// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_automatic_semantic_fallback : given.a_fallback_stage
{
    int _result;

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var execution = new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact }, deadline.Token);
        await _output.Ready.Task.WaitAsync(deadline.Token);
        await deadline.CancelAsync();
        _result = await execution;
        _arguments = await File.ReadAllLinesAsync(_dockerArguments);
    }

    [Unix.Fact] void should_start_exactly_two_containers() => _arguments.Count(argument => argument == "run").ShouldEqual(2);
    [Unix.Fact] void should_stop_the_first_container_before_starting_the_second() => Array.IndexOf(_arguments, "stop").ShouldBeLessThan(Array.LastIndexOf(_arguments, "run"));
    [Unix.Fact] void should_select_semantic_for_the_second_container() => _arguments.ShouldContain("Stage__Runtime__Engine=semantic");
    [Unix.Fact] void should_keep_the_same_ports() => _arguments.Count(argument => argument == $"{_port}:9090").ShouldEqual(2);
    [Unix.Fact] void should_stop_both_containers() => _arguments.Count(argument => argument == "stop").ShouldEqual(2);
    [Unix.Fact] void should_exit_cleanly_after_the_ready_session_is_stopped() => _result.ShouldEqual(ExitCodes.Success);
    [Unix.Fact] void should_not_report_the_first_engine_as_a_failure() => _error.ToString().ShouldEqual(string.Empty);

    [Unix.Fact]
    void should_emit_one_ready_document_with_the_fallback_reason()
    {
        using var json = JsonDocument.Parse(_output.ToString());
        json.RootElement.GetProperty("engine").GetString().ShouldEqual("semantic");
        var fallback = json.RootElement.GetProperty("engineFallback");
        fallback.GetProperty("from").GetString().ShouldEqual("eventmodel");
        fallback.GetProperty("to").GetString().ShouldEqual("semantic");
        fallback.GetProperty("reason").GetString()!.ShouldContain("preserve runsAs");
    }
}
