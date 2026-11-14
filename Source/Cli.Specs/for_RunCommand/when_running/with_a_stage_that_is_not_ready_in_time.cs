// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_stage_that_is_not_ready_in_time : given.a_serving_stage
{
    int _result;

    void Establish() => _statusCode = 503;

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Timeout = 1 }, deadline.Token);
    }

    [Unix.Fact] void should_exit_with_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Unix.Fact] void should_report_the_timeout() => _error.ToString().ShouldContain("not ready within 1 seconds");
    [Unix.Fact] void should_stop_the_container_it_started() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
}
