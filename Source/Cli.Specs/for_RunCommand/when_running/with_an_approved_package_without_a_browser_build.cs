// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_approved_package_without_a_browser_build : given.a_model_naming_a_custom_package
{
    int _result;

    async Task Because() => _result = await Run(_folder, "--package-set", _packageSet);

    [Fact] void should_refuse_the_run() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_which_package_has_no_browser_build() => _error.ToString().ShouldContain("Acme.Charts");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
