// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
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
        _services.Namespaces.AllNamespaces(Arg.Is<AllNamespacesRequest>(request => request.EventStore == "store")).Returns(
            QueryResult<IEnumerable<NamespaceNamesResponse>>.Success(Guid.Empty, [new NamespaceNamesResponse { Name = "tenant-one" }]));
        _services.Namespaces.AllNamespaces(Arg.Is<AllNamespacesRequest>(request => request.EventStore == "unavailable-store")).Returns(
            QueryResult<IEnumerable<NamespaceNamesResponse>>.Error(Guid.Empty, new Exception("Cannot list namespaces")));
    }

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_mark_the_report_as_aggregated() => _data.IsAggregate.ShouldBeTrue();
    [Fact] void should_not_report_a_top_level_tail() => _data.EventSequenceTail.ShouldBeNull();
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_omit_the_null_top_level_json_tail(string format) => JsonDocument.Parse(_outputs[format]).RootElement.TryGetProperty("eventSequenceTail", out _).ShouldBeFalse();
    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_preserve_the_successful_scopes_json_tail(string format) => JsonDocument.Parse(_outputs[format]).RootElement.GetProperty("scopes")[0].GetProperty("eventSequenceTail").GetUInt64().ShouldEqual(10UL);
    [Fact] void should_leave_the_top_level_plain_tail_empty() => _outputs[OutputFormats.Plain].Split(Environment.NewLine).Single(line => line.StartsWith("event_sequence_tail=", StringComparison.Ordinal)).ShouldEqual("event_sequence_tail=");
    [Fact] void should_preserve_the_successful_scopes_plain_tail() => _outputs[OutputFormats.Plain].ShouldContain("scope_event_sequence_tail=10 event_store=store namespace=tenant-one");
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
    [Fact] void should_show_the_successful_scopes_tail_in_text() => _outputs[OutputFormats.Table].ShouldContain("store/tenant-one: event sequence tail: 10");
    [Fact] void should_show_the_successful_scopes_tail_in_watch() => _outputs["watch"].ShouldContain("tail: 10");
    [Fact] void should_identify_the_successful_scope_in_watch() => _outputs["watch"].ShouldContain("store/tenant-one");
    [Fact] void should_preserve_successfully_checked_scopes() => _data.Scopes.Count.ShouldEqual(1);
    [Fact] void should_preserve_successful_event_sequence_tails() => _data.Scopes.All(scope => scope.EventSequenceTail == 10).ShouldBeTrue();
    [Fact] void should_keep_successfully_checked_scopes_healthy() => _data.Scopes.All(scope => scope.IsHealthy).ShouldBeTrue();
    [Fact] void should_identify_the_blocked_store() => _data.ChecksCouldNotRun.All(check => check.EventStore == "unavailable-store").ShouldBeTrue();
    [Fact] void should_preserve_the_discovery_reason() => _data.ChecksCouldNotRun.Single(check => check.Check == "Namespaces").Reason.ShouldContain("Cannot list namespaces");
}
