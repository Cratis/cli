// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach_cancelled_while_creating : given.a_serving_stage
{
    int _result;
    bool _cancelledDuringCreation;

    async Task Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        await File.WriteAllTextAsync(Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\n" +
            $"case \"$1\" in\nrun) : > '{Path.Combine(_tools, "creating")}'; read stopped < '{Path.Combine(_tools, "stop-pipe")}' ;;\nstop) exit 0 ;;\nps) exit 0 ;;\nesac\n");
    }

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var creating = new FileSystemWatcher(_tools, "creating");
        creating.Created += (_, _) =>
        {
            _cancelledDuringCreation = true;
            cancellation.Cancel();
        };
        creating.EnableRaisingEvents = true;
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Detach = true }, cancellation.Token);
    }

    [Unix.Fact] void should_cancel_during_creation() => _cancelledDuringCreation.ShouldBeTrue();
    [Unix.Fact] void should_end_cleanly() => _result.ShouldEqual(ExitCodes.Success);
    [Unix.Fact] void should_stop_the_container_it_may_have_created() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
    [Unix.Fact] void should_not_follow_logs() => DockerArguments.ShouldNotContain("logs");
    [Unix.Fact] void should_stop_by_the_created_name() => DockerArguments[DockerArguments.ToList().IndexOf("stop") + 1].ShouldEqual(DockerArguments[DockerArguments.ToList().IndexOf("--name") + 1]);
}
