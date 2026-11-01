// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_NamespaceObservations.when_reading;

public class and_the_token_is_rejected_by_the_transport : given.namespace_observations
{
    void Establish() => ObserversThrow(StatusCode.Unauthenticated, "Bad gRPC response. HTTP status code: 401");

    async Task Because() => _failure = await Catch.Exception(() => _observations.Consumers("Library", "Default", CancellationToken.None));

    [Fact] void should_leave_it_to_the_token_refresh() => _failure.ShouldBeOfExactType<RpcException>();
}
