// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_all_observer_states_are_present : given.captured_reports
{
    void Establish() => _data = _data with
    {
        ServerVersion = "1.0.0",
        LatestServerVersion = "2.0.0",
        EventStores = ["first", "second"],
        ActiveObservers = 1,
        ReplayingObservers = 2,
        SuspendedObservers = 3,
        DisconnectedObservers = 4,
        QuarantinedObservers = 5,
        TotalObservers = 15
    };

    void Because() => CaptureReports();

    [Fact] void should_list_all_observer_states_in_order() => _outputs[OutputFormats.Table].ShouldContain("1 active  2 replaying  3 suspended  4 disconnected  5 quarantined");
    [Fact] void should_list_the_event_stores_in_order() => _outputs[OutputFormats.Table].ShouldContain("2 stores: first, second");
    [Fact] void should_report_the_available_update() => _outputs[OutputFormats.Table].ShouldContain("1.0.0  update available: 2.0.0");
    [Fact] void should_keep_the_plain_observer_counts() => _outputs[OutputFormats.Plain].ShouldContain("observers_quarantined=5");
    [Fact] void should_mark_watch_health_as_unhealthy() => WatchIcons("Health").Single().Text.ShouldEqual("✗");
}
