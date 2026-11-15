// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_detach_and_sample_data : given.a_model_with_sample_data
{
    int _result;

    async Task Because() => _result = await Run(_folder, "--sample-data", "--detach");

    [Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_why() => _error.ToString().ShouldContain("--detach is not supported");
    [Fact] void should_never_reach_docker() => DockerArguments.ShouldBeEmpty();
}
