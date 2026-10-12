// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_profile_naming_an_approved_package : given.a_model_naming_a_custom_package
{
    int _result;

    void Establish() => AddPackageFolder();

    async Task Because() => _result = await Run(_folder, "--tag", "1.2.0", "--port", "9191", "--workbench-port", "35001", "--package-set", _packageSet);

    [Unix.Fact] void should_mount_the_set_read_only_where_the_stage_reads_it() => DockerArguments.ShouldContain($"{_packageSet}:/packages/approved-packages.json:ro");
    [Unix.Fact] void should_mount_the_package_folder_read_only_under_its_npm_name() => DockerArguments.ShouldContain($"{Path.Combine(_approved, "@acme", "scene.charts")}:/packages/@acme/scene.charts:ro");
    [Unix.Fact] void should_still_mount_the_model_read_only() => DockerArguments.ShouldContain($"{_folder}:/eventmodel:ro");
    [Unix.Fact] void should_run_the_image_last() => DockerArguments[^1].ShouldEqual("cratis/stage:1.2.0");
    [Unix.Fact] void should_report_that_the_container_stopped_before_it_was_ready() => _result.ShouldEqual(ExitCodes.ServerError);
}
