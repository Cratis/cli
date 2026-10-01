// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_all_event_stores_are_selected : given.healthy_services
{
    void Establish()
    {
        _settings.AllEventStores = true;
        _services.EventStores.AllEventStores().Returns(QueryResult<IEnumerable<EventStoreNamesResponse>>.Success(Guid.Empty,
            [new EventStoreNamesResponse { Name = "store" }, new EventStoreNamesResponse { Name = "other" }]));
        _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromResult<IEnumerable<ObserverInformation>>(
        [
            new ObserverInformation { Id = "active", RunningState = ObserverRunningState.Active },
            new ObserverInformation { Id = "replaying", RunningState = ObserverRunningState.Replaying },
            new ObserverInformation { Id = "suspended", RunningState = ObserverRunningState.Suspended },
            new ObserverInformation { Id = "disconnected", RunningState = ObserverRunningState.Disconnected },
            new ObserverInformation { Id = "quarantined", RunningState = ObserverRunningState.Quarantined }
        ]));
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_check_both_namespaces_in_each_store() => _data.Scopes.Count.ShouldEqual(4);
    [Fact] void should_aggregate_quarantined_observers() => _data.QuarantinedObservers.ShouldEqual(4);
    [Fact] void should_aggregate_observers_in_all_states() => _data.TotalObservers.ShouldEqual(20);
    [Fact] void should_count_all_states_in_each_scope() => _data.Scopes.Select(x => x.TotalObservers).ShouldContainOnly(5, 5, 5, 5);
    [Fact] void should_name_both_stores() => _data.Findings.Select(x => x.EventStore).Distinct().ShouldContainOnly("store", "other");
    [Fact] void should_name_both_namespaces() => _data.Findings.Select(x => x.Namespace).Distinct().ShouldContainOnly("tenant-one", "tenant-two");
}
