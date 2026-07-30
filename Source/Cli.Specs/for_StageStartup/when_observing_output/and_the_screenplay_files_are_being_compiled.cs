// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_the_screenplay_files_are_being_compiled : given.a_stage_startup
{
    void Establish() => _startup.Observe("Starting Chronicle (in-memory storage)...");

    void Because() => _startup.Observe("Chronicle is ready.");

    [Fact] void should_move_to_compiling_the_event_model() => _startup.Phase.ShouldEqual(StagePhase.CompilingEventModel);
    [Fact] void should_report_that_the_screenplay_files_are_being_compiled() => _startup.Status.ShouldEqual("Compiling Screenplay files");
}
