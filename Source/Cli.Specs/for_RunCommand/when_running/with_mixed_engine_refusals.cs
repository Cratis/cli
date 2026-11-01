// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_mixed_engine_refusals : given.a_serving_stage
{
    int _result;
    string[] _arguments = [];

    void Establish() => _statusBody = """{"state":"unsupported","engine":"eventmodel","issues":[{"capability":"Routing","artifact":"Register","details":"Routes run only on the semantic engine."},{"capability":"World","artifact":"History","details":"History cannot be reconstructed."}]}""";

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Engine = "eventmodel" }, deadline.Token);
        _arguments = await File.ReadAllLinesAsync(_dockerArguments);
    }

    [Unix.Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Unix.Fact] void should_not_restart_the_container() => _arguments.Count(argument => argument == "run").ShouldEqual(1);
    [Unix.Fact] void should_suggest_the_exact_semantic_command() => _error.ToString().ShouldContain($"--engine semantic --port {_port}");
}
