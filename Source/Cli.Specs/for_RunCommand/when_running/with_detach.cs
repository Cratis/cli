// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach : given.a_serving_stage
{
    int _result;

    async Task Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        var pipe = Path.Combine(_tools, "stop-pipe");
        await File.WriteAllTextAsync(Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\n" +
            $"case \"$1\" in\nrun) printf '%s\\n' 'cratis-container-id' ;;\nlogs) printf '%s\\n' 'Application started'; read stopped < '{pipe}' ;;\n" +
            "stop) exit 0 ;;\nps) exit 0 ;;\nesac\n");
    }

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Detach = true }, deadline.Token);
    }

    [Unix.Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Unix.Fact] void should_start_the_container_in_the_background() => DockerArguments[DockerArguments.ToList().IndexOf("run") + 1].ShouldEqual("-d");
    [Unix.Fact] void should_leave_the_container_running() => DockerArguments.ShouldNotContain("stop");
}
