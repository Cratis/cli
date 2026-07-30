// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_chronicle_is_starting : given.a_stage_startup
{
    void Because() => _startup.Observe("Starting Chronicle (in-memory storage)...");

    [Fact] void should_move_to_starting_chronicle() => _startup.Phase.ShouldEqual(StagePhase.StartingChronicle);
    [Fact] void should_report_that_chronicle_is_starting() => _startup.Status.ShouldEqual("Starting Chronicle");
}
