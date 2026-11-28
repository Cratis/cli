// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_namespaces_query_fails : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish()
    {
        FailedPartitions();
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>())
            .Returns(QueryResult<IEnumerable<NamespaceNamesResponse>>.Error(Guid.Empty, new Exception("storage unavailable")));
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_not_submit() => _observers.DidNotReceive().ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
}
