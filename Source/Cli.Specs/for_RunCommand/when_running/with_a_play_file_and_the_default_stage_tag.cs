// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_play_file_and_the_default_stage_tag : given.a_run_command
{
    string _path;
    int _result;

    void Establish()
    {
        _path = Path.Combine(_folder, "invoicing.play");
        File.WriteAllText(_path, "domain Invoicing\n");
    }

    async Task Because() => _result = await Run(_path, "--port", "9191", "--workbench-port", "35001");

    [Unix.Fact] void should_start_the_released_stage_image_that_matches_the_renderer_packages() =>
        DockerArguments.ShouldContain($"cratis/stage:{StageContainer.DefaultTag}");

    [Unix.Fact] void should_run_stage_4492_by_default() =>
        DockerArguments.ShouldContain("cratis/stage:4.49.2");

    [Unix.Fact] void should_mount_only_the_selected_file_read_only() =>
        DockerArguments.ShouldContain($"{_path}:/eventmodel/input.play:ro");

    [Unix.Fact] void should_report_that_the_container_stopped_before_it_was_ready() => _result.ShouldEqual(ExitCodes.ServerError);
}
