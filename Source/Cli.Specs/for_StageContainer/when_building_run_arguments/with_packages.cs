// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_packages : Specification
{
    IReadOnlyList<string> _folder;
    IReadOnlyList<string> _file;

    void Because()
    {
        var packages = new StagePackageMounts("/approved/approved-packages.json", [("@acme/scene.charts", "/approved/@acme/scene.charts")]);
        _folder = StageContainer.BuildRunArguments("/work/space", "latest", 9090, 35000, "cratis-stage-abc123", packages);
        _file = StageContainer.BuildRunArgumentsForFile("/work/space/a.play", "latest", 9090, 35000, "cratis-stage-abc123", packages);
    }

    [Fact] void should_mount_the_set_read_only() => _folder.ShouldContain("/approved/approved-packages.json:/packages/approved-packages.json:ro");
    [Fact] void should_mount_each_package_read_only_under_its_npm_name() => _folder.ShouldContain("/approved/@acme/scene.charts:/packages/@acme/scene.charts:ro");
    [Fact] void should_still_mount_the_folder_read_only() => _folder.ShouldContain("/work/space:/eventmodel:ro");
    [Fact] void should_run_the_tagged_image_last() => _folder[^1].ShouldEqual("cratis/stage:latest");
    [Fact] void should_mount_the_packages_for_a_single_file_too() => _file.ShouldContain("/approved/@acme/scene.charts:/packages/@acme/scene.charts:ro");
    [Fact] void should_hand_a_single_file_to_the_stage_after_the_image() => _file[^1].ShouldEqual("/eventmodel/input.play");
}
