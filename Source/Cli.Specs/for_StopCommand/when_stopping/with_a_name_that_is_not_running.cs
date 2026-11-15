// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class with_a_name_that_is_not_running : given.a_docker_with_sandboxes
{
    int _result;

    void Establish() => Install("cratis-stage-aaa");

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = await Stop("cratis-stage-zzz");
    }

    [Unix.Fact] void should_report_not_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Unix.Fact] void should_not_stop_anything() => DockerArguments.ShouldNotContain("stop");
}
