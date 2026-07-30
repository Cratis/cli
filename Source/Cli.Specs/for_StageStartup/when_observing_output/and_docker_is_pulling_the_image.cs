// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_docker_is_pulling_the_image : given.a_stage_startup
{
    bool _changed;

    void Because() => _changed = _startup.Observe("Unable to find image 'cratis/stage:latest' locally");

    [Fact] void should_move_to_pulling() => _startup.Phase.ShouldEqual(StagePhase.Pulling);
    [Fact] void should_report_a_change() => _changed.ShouldBeTrue();
    [Fact] void should_name_the_image_being_pulled() => _startup.Status.ShouldEqual("Pulling cratis/stage:latest");
}
