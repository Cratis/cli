// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_profile_naming_an_unapproved_package : given.a_model_naming_a_custom_package
{
    int _result;

    async Task Because() => _result = await Run(_folder);

    [Fact] void should_refuse_the_run() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_unknown_package_with_the_render_code() => _error.ToString().ShouldContain("CLI-RENDER-007");
    [Fact] void should_say_how_to_approve_the_package() => _error.ToString().ShouldContain("--package-set");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
