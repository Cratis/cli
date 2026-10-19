// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_NamespaceObservations.when_reading;

public class and_the_observers_listing_is_unauthenticated : given.namespace_observations
{
    void Establish() => ObserversThrow(StatusCode.Unauthenticated);

    async Task Because() => _failure = await Catch.Exception(() => _observations.Consumers("Library", "Default", CancellationToken.None));

    [Fact] void should_throw_a_read_failure() => _failure.ShouldBeOfExactType<ChronicleReadFailed>();
    [Fact] void should_treat_it_as_unauthorized() => ((ChronicleReadFailed)_failure!).Unauthorized.ShouldBeTrue();
}
