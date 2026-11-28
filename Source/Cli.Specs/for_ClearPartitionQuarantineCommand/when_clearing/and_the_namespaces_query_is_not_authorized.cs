// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.for_ClearPartitionQuarantineCommand.when_clearing;

public class and_the_namespaces_query_is_not_authorized : given.a_clear_partition_quarantine_command
{
    int _result;

    void Establish()
    {
        FailedPartitions();
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>())
            .Returns(new QueryResult<IEnumerable<NamespaceNamesResponse>> { IsAuthorized = false });
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_submit_to_the_server() => _observers.Received(1).ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantineContract>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_the_server_outcome() => _result.ShouldEqual(ExitCodes.Success);
}
