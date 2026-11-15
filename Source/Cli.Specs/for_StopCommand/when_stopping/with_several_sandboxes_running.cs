// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class with_several_sandboxes_running : given.a_docker_with_sandboxes
{
    int _result;

    void Establish() => Install("cratis-stage-aaa", "cratis-stage-bbb");

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = await Stop();
    }

    [Unix.Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Unix.Fact] void should_list_them() => _error.ToString().ShouldContain("cratis-stage-bbb");
    [Unix.Fact] void should_stop_nothing() => DockerArguments.ShouldNotContain("stop");
}
