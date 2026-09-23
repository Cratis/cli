// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_play_file : given.a_run_command
{
    string _path;
    int _result;

    void Establish()
    {
        _path = Path.Combine(_folder, "invoicing model.play");
        File.WriteAllText(_path, "domain Invoicing\n");
        File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");
    }

    async Task Because() => _result = await Run(_path, "--tag", "1.2.0", "--port", "9191", "--workbench-port", "35001");

    [Unix.Fact] void should_start_docker_once_with_only_the_file_mounted_read_only_and_passed_to_the_stage() => DockerArguments.Where((_, index) => index != 3).SequenceEqual(["run", "--rm", "--name", "-p", "9191:9090", "-p", "35001:35000", "-v", $"{_path}:/eventmodel/input.play:ro", "cratis/stage:1.2.0", "/eventmodel/input.play"]).ShouldBeTrue();
    [Unix.Fact] void should_name_the_container() => DockerArguments[3].StartsWith(StageContainer.NamePrefix, StringComparison.Ordinal).ShouldBeTrue();
    [Unix.Fact] void should_report_that_the_container_stopped_before_it_was_ready() => _result.ShouldEqual(ExitCodes.ServerError);
}
