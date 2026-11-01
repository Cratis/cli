// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_an_engine : Specification
{
    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    void should_select_the_engine_before_the_image(string engine) =>
        StageContainer.BuildRunArguments("/work/model", "latest", 9090, 35000, "sandbox", engine: engine)
            .SequenceEqual(["run", "--rm", "--name", "sandbox", "-p", "9090:9090", "-p", "35000:35000", "-v", "/work/model:/eventmodel:ro", "-e", $"Stage__Runtime__Engine={engine}", "cratis/stage:latest"]).ShouldBeTrue();
}
