// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.for_RetryPartitionCommand.when_retrying;

/// <summary>
/// An expired or rejected credential is a login failure for the command's authentication handling, not a missing
/// permission to fall back from.
/// </summary>
public class and_the_observers_lookup_is_unauthenticated : given.a_retry_partition_command
{
    Exception _error;

    void Establish() =>
        _observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(Task.FromException<IEnumerable<ObserverInformation>>(new RpcException(new Status(StatusCode.Unauthenticated, "expired"))));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_propagate_the_authentication_failure() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.Unauthenticated);
    [Fact] void should_not_submit_the_retry() => _observers.DidNotReceive().RetryPartition(Arg.Any<RetryPartitionContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
