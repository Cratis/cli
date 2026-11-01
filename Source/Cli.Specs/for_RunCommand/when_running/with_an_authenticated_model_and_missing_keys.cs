// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;
using Cratis.Environments.Secrets;

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_authenticated_model_and_missing_keys : given.an_authenticated_model
{
    int _result;

    async Task Establish()
    {
        var environment = ProjectEnvironment.Create(_project, "development", null, PassphraseSources.Fixed("spec passphrase"));
        await environment.SetAsync("authentication.providers.GitHub.clientId", "github-client", secret: false);
    }

    async Task Because() => _result = await Run(_folder);

    [Fact] void should_refuse_the_run_with_the_validation_exit_code() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_list_the_key_that_is_missing() => _error.ToString().ShouldContain("authentication.providers.GitHub.clientSecret");
    [Fact] void should_not_list_the_key_that_is_set() => _error.ToString().ShouldNotContain("clientId");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
