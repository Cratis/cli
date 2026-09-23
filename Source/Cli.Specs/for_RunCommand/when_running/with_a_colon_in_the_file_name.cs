// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_colon_in_the_file_name : given.a_run_command
{
    int _result;

    void Establish()
    {
        if (!OperatingSystem.IsWindows())
        {
            File.WriteAllText(Path.Combine(_folder, "model:part.play"), "domain Selected\n");
        }
    }

    async Task Because() => _result = await Run(Path.Combine(_folder, "model:part.play"));

    [Fact] void should_fail_validation() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_colon_cannot_be_mounted() => _error.ToString().ShouldContain("contains a colon");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
