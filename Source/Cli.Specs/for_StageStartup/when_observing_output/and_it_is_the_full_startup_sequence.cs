// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

/// <summary>
/// Replays the lines the Stage container actually writes, in order, to prove the startup ends up reporting
/// the Stage as running - the condition the run command holds "Ready" back for.
/// </summary>
public class and_it_is_the_full_startup_sequence : given.a_stage_startup
{
    static readonly string[] _output =
    [
        "Starting Chronicle (in-memory storage)...",
        "Waiting for Chronicle to be ready...",
        "info: Cratis.Chronicle.Server.Kernel[822977482]",
        "      Starting Cratis Chronicle Server - Version 16.11.0.0",
        "Chronicle is ready.",
        "Using event model from Screenplay .play files under /eventmodel",
        "Starting Stage...",
        "  Stage API           http://localhost:9090",
        "info: Cratis.Stage.Host[1160324588]",
        "      Stage running event model 'Invoicing' as event store 'gentle-zephyr'",
        "info: Microsoft.Hosting.Lifetime[14]",
        "      Now listening on: http://0.0.0.0:9090",
        "info: Microsoft.Hosting.Lifetime[0]",
        "      Application started. Press Ctrl+C to shut down.",
        "info: Cratis.Stage.Host[842608415]",
        "      Registered 6 read model(s) and their projections for event store 'gentle-zephyr'"
    ];

    readonly List<StagePhase> _phases = [];

    void Because()
    {
        foreach (var line in _output)
        {
            if (_startup.Observe(line))
            {
                _phases.Add(_startup.Phase);
            }
        }
    }

    [Fact] void should_end_up_running() => _startup.Phase.ShouldEqual(StagePhase.Running);
    [Fact] void should_report_the_event_model() => _startup.EventModel.ShouldEqual("Invoicing");
    [Fact] void should_report_the_event_store() => _startup.EventStore.ShouldEqual("gentle-zephyr");
    [Fact] void should_not_report_an_error() => _startup.Error.ShouldBeNull();
    [Fact] void should_go_through_every_phase_in_order() => _phases.ShouldContainOnly([StagePhase.StartingChronicle, StagePhase.CompilingEventModel, StagePhase.StartingStage, StagePhase.RegisteringReadModels, StagePhase.Running]);
}
