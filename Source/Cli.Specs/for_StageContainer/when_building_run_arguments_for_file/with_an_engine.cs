// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments_for_file;

public class with_an_engine : Specification
{
    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    void should_select_the_engine_before_the_image_and_input(string engine) =>
        StageContainer.BuildRunArgumentsForFile("/work/model.play", "latest", 9090, 35000, "sandbox", engine: engine)
            .SequenceEqual(["run", "--rm", "--name", "sandbox", "-p", "9090:9090", "-p", "35000:35000", "-v", "/work/model.play:/eventmodel/input.play:ro", "-e", $"Stage__Runtime__Engine={engine}", "cratis/stage:latest", "/eventmodel/input.play"]).ShouldBeTrue();
}
