// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_spaces_in_the_folder_path : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildRunArguments("/work/my models", "1.2.0", 9191, 35001, "cratis-stage-abc123");

    [Fact] void should_preserve_the_folder_invocation_order_except_for_read_only_mounting() => _arguments.SequenceEqual(["run", "--rm", "--name", "cratis-stage-abc123", "-p", "9191:9090", "-p", "35001:35000", "-v", "/work/my models:/eventmodel:ro", "cratis/stage:1.2.0"]).ShouldBeTrue();
    [Fact] void should_keep_the_mount_in_one_unquoted_argument() => _arguments[9].ShouldEqual("/work/my models:/eventmodel:ro");
    [Fact] void should_keep_the_image_as_the_last_argument() => _arguments[^1].ShouldEqual("cratis/stage:1.2.0");
}
