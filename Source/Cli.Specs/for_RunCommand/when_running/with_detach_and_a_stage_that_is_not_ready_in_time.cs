// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach_and_a_stage_that_is_not_ready_in_time : given.a_serving_stage
{
    int _result;

    async Task Establish()
    {
        _statusCode = 503;
        if (OperatingSystem.IsWindows()) return;
        var pipe = Path.Combine(_tools, "stop-pipe");
        await File.WriteAllTextAsync(Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\n" +
            $"case \"$1\" in\nrun) printf '%s\\n' 'cratis-container-id' ;;\nlogs) printf '%s\\n' 'Starting'; read stopped < '{pipe}' ;;\n" +
            $"stop) printf '%s\\n' stopped > '{pipe}' ;;\nps) exit 0 ;;\nesac\n");
    }

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Detach = true, Timeout = 1 }, deadline.Token);
    }

    [Unix.Fact] void should_exit_with_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Unix.Fact] void should_stop_the_container_it_created() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
}
