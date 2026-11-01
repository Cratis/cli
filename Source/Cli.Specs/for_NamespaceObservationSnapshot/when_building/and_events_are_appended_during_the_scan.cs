// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class and_events_are_appended_during_the_scan : given.a_namespace
{
    void Establish()
    {
        Appended(15, _register);
        _tail = 9;
    }

    async Task Because() => await Build();

    [Fact] void should_never_read_past_the_captured_tail() => _pages.SelectMany(page => page).Max().ShouldEqual(9UL);
    [Fact] void should_record_the_captured_tail() => _snapshot.Coverage.CapturedTail.ShouldEqual(9L);
    [Fact] void should_examine_up_to_the_captured_tail() => _snapshot.Coverage.LastExamined.ShouldEqual(9L);
    [Fact] void should_count_only_events_up_to_the_captured_tail() => Appends("Registered").ShouldEqual(10L);
    [Fact] void should_have_full_coverage() => _snapshot.Coverage.Complete.ShouldBeTrue();
}
