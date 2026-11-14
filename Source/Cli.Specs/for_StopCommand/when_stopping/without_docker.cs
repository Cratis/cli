// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class without_docker : given.a_docker_with_sandboxes
{
    int _result;

    async Task Because() => _result = await Stop();

    [Fact] void should_exit_with_a_connection_error() => _result.ShouldEqual(ExitCodes.ConnectionError);
    [Fact] void should_say_docker_is_not_available() => _error.ToString().ShouldContain("Docker is not available");
}
