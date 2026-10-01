// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_DiagnoseCommand.given;

public class healthy_services : captured_reports
{
    protected IServices _services;
    protected DiagnoseSettings _settings;

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _settings = new DiagnoseSettings { Server = "chronicle://localhost:35000", EventStore = "store", Namespace = "tenant-one" };

        // No version is supplied by the double, so gathering never contacts the advisory package feed.
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = null! });
        _services.EventStores.AllEventStores().Returns(QueryResult<IEnumerable<EventStoreNamesResponse>>.Success(Guid.Empty,
            [new EventStoreNamesResponse { Name = "store" }]));
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>()).Returns(QueryResult<IEnumerable<NamespaceNamesResponse>>.Success(Guid.Empty,
            [new NamespaceNamesResponse { Name = "tenant-one" }, new NamespaceNamesResponse { Name = "tenant-two" }]));
        _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromResult<IEnumerable<ObserverInformation>>([]));
        _services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>()).Returns(Task.FromResult<IEnumerable<FailedPartition>>([]));
        _services.Recommendations.GetRecommendations(Arg.Any<GetRecommendationsRequest>()).Returns(QueryResult<IEnumerable<RecommendationDetailsResponse>>.Success(Guid.Empty, []));
        _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty,
            new EventSequenceTailResponse { SequenceNumber = 10 }));
    }
}
