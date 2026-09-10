// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_non_play_file : given.a_run_command
{
    int _result;

    void Establish()
    {
        _settings.Path = Path.Combine(_folder, "implementation.cs");
        File.WriteAllText(_settings.Path, "// Not a Screenplay document");
        File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_file_before_attempting_docker() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_use_the_parent_folder_instead() => _error.ToString().ShouldContain("is not a Screenplay (.play) file");
}
