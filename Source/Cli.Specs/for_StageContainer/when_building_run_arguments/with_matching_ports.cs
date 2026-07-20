// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_matching_ports : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildRunArguments("/work/space", "latest", 9090);

    [Fact] void should_run_a_container() => _arguments.ShouldContain("run");
    [Fact] void should_remove_the_container_on_exit() => _arguments.ShouldContain("--rm");
    [Fact] void should_publish_the_host_port_to_the_api_port() => _arguments.ShouldContain("9090:9090");
    [Fact] void should_mount_the_folder_at_the_model_path() => _arguments.ShouldContain("/work/space:/eventmodel");
    [Fact] void should_run_the_tagged_image() => _arguments.ShouldContain("cratis/stage:latest");
}
