// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_the_connection_fails : given.a_conform_namespace_command
{
    Exception? _failure;

    void Establish() => _observations.EventStores(Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<string>>>(_ => throw new RpcException(new Status(StatusCode.Unavailable, "unreachable")));

    async Task Because() => _failure = await Catch.Exception(Run);

    [Fact] void should_leave_the_connection_failure_to_the_connection_handler() => _failure.ShouldBeOfExactType<RpcException>();
    [Fact] void should_not_write_a_report() => _output.ShouldBeEmpty();
}
