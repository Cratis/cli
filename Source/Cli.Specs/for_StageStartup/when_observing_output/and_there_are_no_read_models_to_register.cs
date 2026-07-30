// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_there_are_no_read_models_to_register : given.a_stage_startup
{
    void Establish() => _startup.Observe("      Application started. Press Ctrl+C to shut down.");

    void Because() => _startup.Observe("      Event model 'Invoicing' has no read models with projections to register");

    [Fact] void should_move_to_running() => _startup.Phase.ShouldEqual(StagePhase.Running);
}
