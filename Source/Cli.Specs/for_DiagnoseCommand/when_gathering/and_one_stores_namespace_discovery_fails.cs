// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_one_stores_namespace_discovery_fails : given.blocked_discovery
{
    void Establish()
    {
        _settings.AllEventStores = true;
        _services.EventStores.AllEventStores().Returns(QueryResult<IEnumerable<EventStoreNamesResponse>>.Success(Guid.Empty,
            [new EventStoreNamesResponse { Name = "store" }, new EventStoreNamesResponse { Name = "unavailable-store" }]));
        _services.Namespaces.AllNamespaces(Arg.Is<AllNamespacesRequest>(request => request.EventStore == "unavailable-store")).Returns(
            QueryResult<IEnumerable<NamespaceNamesResponse>>.Error(Guid.Empty, new Exception("Cannot list namespaces")));
    }

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_return_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_mark_blocked_text_rows_with_a_cross() => TextRowsAreUnavailable().ShouldBeTrue();
    [Fact] void should_mark_blocked_watch_rows_with_red_crosses() => WatchRowsAreUnavailable().ShouldBeTrue();
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_record_all_blocked_queries_in_json(string format) => BlockedJsonQueries(format).ShouldEqual(4);
    [Fact] void should_record_all_blocked_queries_in_plain_output() => BlockedPlainQueries().ShouldEqual(4);
    [Fact] void should_not_claim_a_verified_absence_of_quarantine_in_text() => _outputs[OutputFormats.Table].ShouldNotContain("0 quarantined (known count)");
    [Fact] void should_not_claim_a_verified_absence_of_quarantine_in_watch() => _outputs["watch"].ShouldNotContain("0 quarantined (known count)");
    [Fact] void should_preserve_successfully_checked_scopes() => _data.Scopes.Count.ShouldEqual(2);
    [Fact] void should_preserve_successful_event_sequence_tails() => _data.Scopes.All(scope => scope.EventSequenceTail == 10).ShouldBeTrue();
    [Fact] void should_keep_successfully_checked_scopes_healthy() => _data.Scopes.All(scope => scope.IsHealthy).ShouldBeTrue();
    [Fact] void should_identify_the_blocked_store() => _data.ChecksCouldNotRun.All(check => check.EventStore == "unavailable-store").ShouldBeTrue();
    [Fact] void should_preserve_the_discovery_reason() => _data.ChecksCouldNotRun.Single(check => check.Check == "Namespaces").Reason.ShouldContain("Cannot list namespaces");
}
