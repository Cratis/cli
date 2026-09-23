// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_missing_file : given.a_run_command
{
    int _result;

    void Establish() => File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");

    async Task Because() => _result = await Run(Path.Combine(_folder, "missing.play"));

    [Fact] void should_fail_validation() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_path_does_not_exist() => _error.ToString().ShouldContain("does not exist");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
