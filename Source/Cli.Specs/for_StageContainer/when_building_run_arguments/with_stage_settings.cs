// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_stage_settings : Specification
{
    IReadOnlyList<string> _folder;
    IReadOnlyList<string> _file;

    void Because()
    {
        string[] settings = ["Sandbox:Users=true", "Extensions:Packages:0:Name=hello world=again; --privileged"];
        _folder = StageContainer.BuildRunArguments("/models", "1.2.3", 9090, 35000, "test", stageSettings: settings);
        _file = StageContainer.BuildRunArgumentsForFile("/models/app.play", "1.2.3", 9090, 35000, "test", stageSettings: settings);
    }

    [Fact] void should_map_the_key_for_a_folder() => _folder.ShouldContain("Stage__Sandbox__Users=true");
    [Fact] void should_map_the_key_for_a_single_file() => _file.ShouldContain("Stage__Sandbox__Users=true");
    [Fact] void should_keep_values_as_a_single_argument() => _folder.ShouldContain("Stage__Extensions__Packages__0__Name=hello world=again; --privileged");
    [Fact] void should_not_inject_docker_flags() => _folder.ShouldNotContain("--privileged");
    [Fact] void should_keep_the_file_entrypoint_last() => _file[^1].ShouldEqual("/eventmodel/input.play");
    [Fact] void should_put_settings_before_the_image() => _folder[^1].ShouldEqual("cratis/stage:1.2.3");
}
