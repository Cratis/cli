// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class with_attributed_and_unattributed_events : given.a_namespace
{
    static readonly ObservedProducer _reactor = new(ObservedProducerKind.Reaction, "Library.Notify");

    void Establish()
    {
        _registered = [new("Registered", 1), new("Registered", 2)];
        Appended(3, _register);
        Appended(2, _reactor);
        Appended(4);
        Appended(1, type: "Unregistered");
        _tail = 9;
    }

    async Task Because() => await Build();

    [Fact] void should_count_appends_against_the_highest_registered_generation() => _snapshot.Appends.Single(append => append.Type.TypeId == "Registered").Type.Generation.ShouldEqual(2U);
    [Fact] void should_count_an_unregistered_type_as_generation_one() => _snapshot.Appends.Single(append => append.Type.TypeId == "Unregistered").Type.Generation.ShouldEqual(1U);
    [Fact] void should_attribute_command_appends() => _snapshot.Edges.Single(edge => edge.Producer == _register).Events.ShouldEqual(3L);
    [Fact] void should_attribute_reactor_appends() => _snapshot.Edges.Single(edge => edge.Producer == _reactor).Events.ShouldEqual(2L);
    [Fact] void should_count_appends_without_a_producer_as_unattributed() => _snapshot.UnattributedAppends.Single(append => append.Type.TypeId == "Registered").Count.ShouldEqual(4L);
    [Fact] void should_count_every_append() => _snapshot.Appends.Sum(append => append.Count).ShouldEqual(10L);
}
