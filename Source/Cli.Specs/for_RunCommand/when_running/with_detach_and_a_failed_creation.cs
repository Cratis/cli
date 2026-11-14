// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach_and_a_failed_creation : given.a_run_command
{
    int _result;

    async Task Establish() => await File.WriteAllTextAsync(Path.Combine(_folder, "invoicing.play"), "domain Invoicing\n");

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = await Run(_folder, "--detach");
    }

    [Unix.Fact] void should_report_the_failure() => _result.ShouldEqual(ExitCodes.ServerError);
    [Unix.Fact] void should_not_follow_logs() => DockerArguments.ShouldNotContain("logs");
    [Unix.Fact] void should_stop_any_partially_created_container() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
}
