// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class without_a_path_in_an_empty_current_folder : given.a_run_command
{
    int _result;

    async Task Because() => _result = await Run();

    [Fact] void should_fail_validation() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_look_in_the_current_folder() => _error.ToString().ShouldContain("No Screenplay files (.play) found in the folder");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
