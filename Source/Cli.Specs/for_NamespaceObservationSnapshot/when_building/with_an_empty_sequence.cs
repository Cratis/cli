// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservationSnapshot.when_building;

public class with_an_empty_sequence : given.a_namespace
{
    async Task Because() => await Build();

    [Fact] void should_have_no_captured_tail() => _snapshot.Coverage.CapturedTail.ShouldBeNull();
    [Fact] void should_count_the_sequence_as_fully_examined() => _snapshot.Coverage.Complete.ShouldBeTrue();
    [Fact] void should_not_read_any_metadata() => _pages.ShouldBeEmpty();
    [Fact] void should_keep_the_registry() => _snapshot.RegisteredEventTypes.Single().TypeId.ShouldEqual("Registered");
}
