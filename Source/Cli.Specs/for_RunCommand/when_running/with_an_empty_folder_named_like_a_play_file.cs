// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_empty_folder_named_like_a_play_file : given.a_run_command
{
    int _result;

    void Establish() => Directory.CreateDirectory(Path.Combine(_folder, "empty.play"));

    async Task Because() => _result = await Run(Path.Combine(_folder, "empty.play"));

    [Fact] void should_fail_validation() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_treat_it_as_a_folder() => _error.ToString().ShouldContain("No Screenplay files (.play) found in the folder");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
