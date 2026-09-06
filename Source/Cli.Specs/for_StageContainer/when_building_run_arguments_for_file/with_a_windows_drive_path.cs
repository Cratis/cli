// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments_for_file;

public class with_a_windows_drive_path : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildRunArgumentsForFile(@"C:\work\my models\selected.play", "latest", 9090, 35000, "cratis-stage-abc123");

    [Fact] void should_preserve_the_drive_prefix_and_backslashes_without_shell_escaping() => _arguments[9].ShouldEqual(@"C:\work\my models\selected.play:/eventmodel/input.play:ro");
    [Fact] void should_keep_the_default_ports_and_argument_order() => _arguments.SequenceEqual(["run", "--rm", "--name", "cratis-stage-abc123", "-p", "9090:9090", "-p", "35000:35000", "-v", @"C:\work\my models\selected.play:/eventmodel/input.play:ro", "cratis/stage:latest", "/eventmodel/input.play"]).ShouldBeTrue();
}
