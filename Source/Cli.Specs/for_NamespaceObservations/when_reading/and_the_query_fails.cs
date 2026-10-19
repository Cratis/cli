// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NamespaceObservations.when_reading;

public class and_the_query_fails : given.namespace_observations
{
    void Establish() => EventStoresReturn(new() { IsAuthorized = true, ExceptionMessages = ["storage failed"] });

    async Task Because() => _failure = await Catch.Exception(() => _observations.EventStores(CancellationToken.None));

    [Fact] void should_throw_a_read_failure() => _failure.ShouldBeOfExactType<ChronicleReadFailed>();
    [Fact] void should_not_treat_it_as_unauthorized() => ((ChronicleReadFailed)_failure!).Unauthorized.ShouldBeFalse();
    [Fact] void should_carry_the_servers_explanation() => _failure!.Message.ShouldContain("storage failed");
}
