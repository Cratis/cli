// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach_and_a_semantic_refusal : given.a_serving_stage
{
    int _result;

    async Task Establish()
    {
        _statusBody = """{"state":"unsupported","engine":"semantic","issues":[]}""";
        if (OperatingSystem.IsWindows()) return;
        var pipe = Path.Combine(_tools, "stop-pipe");
        await File.WriteAllTextAsync(Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\n" +
            $"case \"$1\" in\nrun) printf '%s\\n' 'cratis-container-id' ;;\nlogs) read stopped < '{pipe}' ;;\n" +
            $"stop) printf '%s\\n' stopped > '{pipe}' ;;\nps) exit 0 ;;\nesac\n");
    }

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Detach = true }, deadline.Token);
    }

    [Unix.Fact] void should_report_the_refusal() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Unix.Fact] void should_stop_the_created_container() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
}
