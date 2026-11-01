// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class and_the_range_spans_several_pages : given.a_namespace
{
    void Establish()
    {
        Appended(1200, _register);
        _tail = 1199;
    }

    async Task Because() => await Build();

    [Fact] void should_read_three_pages() => _pages.Count.ShouldEqual(3);
    [Fact] void should_never_request_more_than_a_page() => _pages.Max(page => page.Count).ShouldEqual(NamespaceObservationScan.PageSize);
    [Fact] void should_count_every_event() => Appends("Registered").ShouldEqual(1200L);
    [Fact] void should_have_full_coverage() => _snapshot.Coverage.Complete.ShouldBeTrue();
}
