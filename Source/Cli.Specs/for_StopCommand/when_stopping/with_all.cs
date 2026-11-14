// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class with_all : given.a_docker_with_sandboxes
{
    int _result;

    void Establish() => Install("cratis-stage-aaa", "cratis-stage-bbb");

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = await Stop("--all");
    }

    [Unix.Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Unix.Fact] void should_stop_every_sandbox() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(2);
}
