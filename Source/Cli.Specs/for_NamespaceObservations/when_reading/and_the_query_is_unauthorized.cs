// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservations.when_reading;

public class and_the_query_is_unauthorized : given.namespace_observations
{
    void Establish() => EventStoresReturn(new() { IsAuthorized = false });

    async Task Because() => _failure = await Catch.Exception(() => _observations.EventStores(CancellationToken.None));

    [Fact] void should_throw_a_read_failure() => _failure.ShouldBeOfExactType<ChronicleReadFailed>();
    [Fact] void should_treat_it_as_unauthorized() => ((ChronicleReadFailed)_failure!).Unauthorized.ShouldBeTrue();
}
