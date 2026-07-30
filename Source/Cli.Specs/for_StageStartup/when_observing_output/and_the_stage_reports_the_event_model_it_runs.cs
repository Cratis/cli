// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_the_stage_reports_the_event_model_it_runs : given.a_stage_startup
{
    void Because() => _startup.Observe("      Stage running event model 'Invoicing' as event store 'gentle-zephyr'");

    [Fact] void should_capture_the_event_model() => _startup.EventModel.ShouldEqual("Invoicing");
    [Fact] void should_capture_the_event_store() => _startup.EventStore.ShouldEqual("gentle-zephyr");
    [Fact] void should_move_to_starting_the_stage() => _startup.Phase.ShouldEqual(StagePhase.StartingStage);
}
