// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_the_registration_failed : given.a_stage_startup
{
    void Establish() => _startup.Observe("      Application started. Press Ctrl+C to shut down.");

    void Because() => _startup.Observe("      Failed to register the event model's read models and projections with Chronicle");

    [Fact] void should_move_to_running_rather_than_wait_for_a_registration_that_will_not_come() => _startup.Phase.ShouldEqual(StagePhase.Running);
}
