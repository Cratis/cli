// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.when_stopping;

[Collection(CliSpecsCollection.Name)]
public class with_a_foreign_container_name : given.a_docker_with_sandboxes
{
    int _result;

    void Establish() => Install("cratis-stage-aaa");

    async Task Because() => _result = await Stop("postgres");

    [Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_never_reach_docker() => DockerArguments.ShouldBeEmpty();
}
