// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class and_max_events_is_reached : given.a_namespace
{
    void Establish()
    {
        Appended(10, _register);
        _tail = 9;
        _scan = _scan with { MaxEvents = 4 };
    }

    async Task Because() => await Build();

    [Fact] void should_count_only_the_maximum() => Appends("Registered").ShouldEqual(4L);
    [Fact] void should_stop_before_the_first_event_beyond_the_maximum() => _snapshot.Coverage.LastExamined.ShouldEqual(3L);
    [Fact] void should_still_record_the_captured_tail() => _snapshot.Coverage.CapturedTail.ShouldEqual(9L);
    [Fact] void should_have_partial_coverage() => _snapshot.Coverage.Complete.ShouldBeFalse();
}
