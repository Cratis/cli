// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_detach : Specification
{
    IReadOnlyList<string> _arguments;
    IReadOnlyList<string> _fileArguments;
    IReadOnlyList<string> _foreground;

    void Because()
    {
        _arguments = StageContainer.BuildRunArguments("/work/space", "1.2.0", 9090, 35000, "cratis-stage-abc123", detached: true);
        _fileArguments = StageContainer.BuildRunArgumentsForFile("/work/space/a.play", "1.2.0", 9090, 35000, "cratis-stage-abc123", detached: true);
        _foreground = StageContainer.BuildRunArguments("/work/space", "1.2.0", 9090, 35000, "cratis-stage-abc123");
    }

    [Fact] void should_run_in_the_background_right_after_run() => _arguments.Take(2).ShouldContainOnly("run", "-d");
    [Fact] void should_run_a_file_in_the_background_right_after_run() => _fileArguments.Take(2).ShouldContainOnly("run", "-d");
    [Fact] void should_still_remove_the_container_when_it_stops() => _arguments.ShouldContain("--rm");
    [Fact] void should_not_detach_by_default() => _foreground.ShouldNotContain("-d");
}
