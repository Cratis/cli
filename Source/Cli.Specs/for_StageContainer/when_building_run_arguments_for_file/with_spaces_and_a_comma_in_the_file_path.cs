// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments_for_file;

public class with_spaces_and_a_comma_in_the_file_path : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildRunArgumentsForFile("/work/my models/selected,model.PlAy", "1.2.0", 9191, 35001, "cratis-stage-abc123");

    [Fact] void should_build_the_exact_ordered_invocation() => _arguments.SequenceEqual(["run", "--rm", "--name", "cratis-stage-abc123", "-p", "9191:9090", "-p", "35001:35000", "-v", "/work/my models/selected,model.PlAy:/eventmodel/input.play:ro", "cratis/stage:1.2.0", "/eventmodel/input.play"]).ShouldBeTrue();
    [Fact] void should_mount_only_the_exact_file_read_only_in_one_argument() => _arguments[9].ShouldEqual("/work/my models/selected,model.PlAy:/eventmodel/input.play:ro");
    [Fact] void should_place_the_container_input_path_immediately_after_the_image() => _arguments.Skip(10).SequenceEqual(["cratis/stage:1.2.0", "/eventmodel/input.play"]).ShouldBeTrue();
    [Fact] void should_not_mount_the_host_parent_folder() => _arguments.ShouldNotContain("/work/my models:/eventmodel:ro");
    [Fact] void should_have_only_one_mount() => _arguments.Count(argument => argument == "-v").ShouldEqual(1);
}
