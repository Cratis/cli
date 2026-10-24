// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_sample_data : Specification
{
    [Fact]
    void should_mount_the_original_folder_and_seed_as_read_only_siblings() =>
        StageContainer.BuildRunArguments("/work/my models", "1.2.0", 9191, 35001, "sandbox", sampleDataFile: "/tmp/my seeds/seed.play")
            .SequenceEqual(["run", "--rm", "--name", "sandbox", "-p", "9191:9090", "-p", "35001:35000", "-v", "/work/my models:/eventmodel/model:ro", "-v", "/tmp/my seeds/seed.play:/eventmodel/cratis-sample-data.play:ro", "-e", "Stage__Runtime__Engine=semantic", "cratis/stage:1.2.0"]).ShouldBeTrue();

    [Fact]
    void should_mount_only_the_selected_file_and_seed_and_compile_their_common_parent() =>
        StageContainer.BuildRunArgumentsForFile("/work/my models/model.play", "1.2.0", 9191, 35001, "sandbox", sampleDataFile: "/tmp/my seeds/seed.play")
            .SequenceEqual(["run", "--rm", "--name", "sandbox", "-p", "9191:9090", "-p", "35001:35000", "-v", "/work/my models/model.play:/eventmodel/model/input.play:ro", "-v", "/tmp/my seeds/seed.play:/eventmodel/cratis-sample-data.play:ro", "-e", "Stage__Runtime__Engine=semantic", "cratis/stage:1.2.0"]).ShouldBeTrue();

    [Fact]
    void should_keep_approved_package_mounts_with_the_seed() =>
        StageContainer.BuildRunArguments("/work/model", "latest", 9090, 35000, "sandbox", new("/approved/set.json", [("@acme/charts", "/approved/charts")]), sampleDataFile: "/tmp/seed.play")
            .SequenceEqual(["run", "--rm", "--name", "sandbox", "-p", "9090:9090", "-p", "35000:35000", "-v", "/work/model:/eventmodel/model:ro", "-v", "/tmp/seed.play:/eventmodel/cratis-sample-data.play:ro", "-v", "/approved/set.json:/packages/approved-packages.json:ro", "-v", "/approved/charts:/packages/@acme/charts:ro", "-e", "Stage__Runtime__Engine=semantic", "cratis/stage:latest"]).ShouldBeTrue();
}
