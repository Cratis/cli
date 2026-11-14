// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class with_a_named_sandbox_among_several : given.a_docker_with_sandboxes
{
    int _result;

    void Establish() => Install("cratis-stage-aaa", "cratis-stage-bbb");

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = await Stop("cratis-stage-bbb");
    }

    [Unix.Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Unix.Fact] void should_stop_only_one_sandbox() => DockerArguments.Count(argument => argument == "stop").ShouldEqual(1);
    [Unix.Fact] void should_stop_the_named_sandbox() => DockerArguments[DockerArguments.ToList().IndexOf("stop") + 1].ShouldEqual("cratis-stage-bbb");
}
