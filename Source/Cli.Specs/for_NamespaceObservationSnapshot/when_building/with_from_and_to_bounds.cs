// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class with_from_and_to_bounds : given.a_namespace
{
    void Establish()
    {
        Appended(10, _register);
        _tail = 9;
        _scan = _scan with { From = 2, To = 5 };
    }

    async Task Because() => await Build();

    [Fact] void should_not_start_at_the_first_event() => _snapshot.Coverage.StartsAtFirstEvent.ShouldBeFalse();
    [Fact] void should_examine_from_the_lower_bound() => _snapshot.Coverage.FirstExamined.ShouldEqual(2L);
    [Fact] void should_examine_to_the_upper_bound() => _snapshot.Coverage.LastExamined.ShouldEqual(5L);
    [Fact] void should_only_request_the_bounded_range() => _pages.SelectMany(page => page).ShouldContainOnly([2UL, 3UL, 4UL, 5UL]);
    [Fact] void should_count_the_events_in_range() => Appends("Registered").ShouldEqual(4L);
    [Fact] void should_have_partial_coverage() => _snapshot.Coverage.Complete.ShouldBeFalse();
}
