// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach_and_a_failed_log_follower : given.a_serving_stage
{
    int _result;

    async Task Establish()
    {
        _statusCode = 503;
        if (OperatingSystem.IsWindows()) return;
        await File.WriteAllTextAsync(Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\n" +
            "case \"$1\" in\nrun) printf '%s\\n' 'cratis-container-id' ;;\nlogs) exit 1 ;;\nstop) exit 0 ;;\nps) exit 0 ;;\nesac\n");
    }

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Detach = true }, deadline.Token);
    }

    [Unix.Fact] void should_report_the_failure() => _result.ShouldEqual(ExitCodes.ServerError);
    [Unix.Fact] void should_have_created_the_container_before_following_logs() => DockerArguments.ToList().IndexOf("run").ShouldBeLessThan(DockerArguments.ToList().IndexOf("logs"));
    [Unix.Fact] void should_stop_the_created_container() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
    [Unix.Fact] void should_stop_by_the_created_name() => DockerArguments[DockerArguments.ToList().IndexOf("stop") + 1].ShouldEqual(DockerArguments[DockerArguments.ToList().IndexOf("--name") + 1]);
}
