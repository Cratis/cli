// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_authenticated_model_and_no_environment : given.an_authenticated_model
{
    int _result;

    async Task Because() => _result = await Run(_folder);

    [Fact] void should_refuse_the_run() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_environment_does_not_exist() => _error.ToString().ShouldContain("does not exist, and the model needs 2 key(s)");
    [Fact] void should_list_the_keys_the_model_needs() => _error.ToString().ShouldContain("authentication.providers.GitHub.clientId, authentication.providers.GitHub.clientSecret");
    [Fact] void should_say_how_to_create_it() => _error.ToString().ShouldContain("cratis env init development");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
