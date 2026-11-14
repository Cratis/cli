// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class with_no_sandbox_running : given.a_docker_with_sandboxes
{
    int _result;

    void Establish() => Install();

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = await Stop();
    }

    [Unix.Fact] void should_exit_with_not_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Unix.Fact] void should_say_so() => _error.ToString().ShouldContain("No Stage sandbox is running");
}
