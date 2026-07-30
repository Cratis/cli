// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_the_line_is_not_recognized : given.a_stage_startup
{
    bool _changed;

    void Establish() => _startup.Observe("Starting Chronicle (in-memory storage)...");

    void Because() => _changed = _startup.Observe("warn: Microsoft.AspNetCore.Server.Kestrel[0]");

    [Fact] void should_stay_in_the_phase_it_reached() => _startup.Phase.ShouldEqual(StagePhase.StartingChronicle);
    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
}
