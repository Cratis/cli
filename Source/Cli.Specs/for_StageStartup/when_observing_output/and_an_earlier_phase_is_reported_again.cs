// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_an_earlier_phase_is_reported_again : given.a_stage_startup
{
    bool _changed;

    void Establish()
    {
        _startup.Observe("Starting Chronicle (in-memory storage)...");
        _startup.Observe("Chronicle is ready.");
        _startup.Observe("Starting Stage...");
    }

    void Because() => _changed = _startup.Observe("Waiting for Chronicle to be ready...");

    [Fact] void should_stay_in_the_phase_it_reached() => _startup.Phase.ShouldEqual(StagePhase.StartingStage);
    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
}
