// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_NamespaceObservations.when_reading;

public class and_the_observers_listing_fails_otherwise : given.namespace_observations
{
    void Establish() => ObserversThrow(StatusCode.Internal);

    async Task Because() => _failure = await Catch.Exception(() => _observations.Consumers("Library", "Default", CancellationToken.None));

    [Fact] void should_leave_the_failure_to_the_connection_handler() => _failure.ShouldBeOfExactType<RpcException>();
}
