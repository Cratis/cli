// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_event_store_discovery_fails : given.healthy_services
{
    void Establish()
    {
        _settings.AllEventStores = true;
        _services.EventStores.AllEventStores().Returns(QueryResult<IEnumerable<EventStoreNamesResponse>>.Error(Guid.Empty, new Exception("Cannot list stores")));
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_pass_without_checking_any_store() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_report_the_unavailable_check() => _data.ChecksCouldNotRun.Single().Check.ShouldEqual("Event stores");
    [Fact] void should_preserve_the_reason() => _data.ChecksCouldNotRun.Single().Reason.ShouldContain("Cannot list stores");
    [Fact] void should_not_claim_to_have_checked_any_scope() => _data.Scopes.ShouldBeEmpty();
}
