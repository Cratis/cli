// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_folder_and_the_default_stage_tag : given.a_run_command
{
    int _result;

    void Establish() => File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, "features")).FullName, "invoicing.play"), "domain Invoicing\n");

    async Task Because() => _result = await Run(_folder, "--port", "9191", "--workbench-port", "35001");

    [Unix.Fact] void should_start_the_released_stage_image_that_matches_the_renderer_packages() =>
        DockerArguments.ShouldContain($"cratis/stage:{StageContainer.DefaultTag}");

    [Unix.Fact] void should_run_stage_44910_by_default() =>
        DockerArguments.ShouldContain("cratis/stage:4.51.3");

    [Unix.Fact] void should_mount_only_the_selected_folder_read_only() =>
        DockerArguments.ShouldContain($"{_folder}:/eventmodel:ro");

    [Unix.Fact] void should_report_that_the_container_stopped_before_it_was_ready() => _result.ShouldEqual(ExitCodes.ServerError);
}
