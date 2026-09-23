// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_file_that_is_not_a_screenplay : given.a_run_command
{
    int _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_folder, "implementation.cs"), "// Not a Screenplay document");
        File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");
    }

    async Task Because() => _result = await Run(Path.Combine(_folder, "implementation.cs"));

    [Fact] void should_fail_validation() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_say_the_file_is_not_a_screenplay() => _error.ToString().ShouldContain("is not a Screenplay (.play) file");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
