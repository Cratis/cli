// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class and_max_events_equals_the_event_count : given.a_namespace
{
    void Establish()
    {
        Appended(4, _register);
        _tail = 3;
        _scan = _scan with { MaxEvents = 4 };
    }

    async Task Because() => await Build();

    [Fact] void should_count_every_event() => Appends("Registered").ShouldEqual(4L);
    [Fact] void should_have_full_coverage() => _snapshot.Coverage.Complete.ShouldBeTrue();
}
