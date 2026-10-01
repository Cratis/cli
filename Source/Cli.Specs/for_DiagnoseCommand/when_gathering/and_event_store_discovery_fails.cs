// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_event_store_discovery_fails : given.blocked_discovery
{
    void Establish()
    {
        _settings.AllEventStores = true;
        _services.EventStores.AllEventStores().Returns(QueryResult<IEnumerable<EventStoreNamesResponse>>.Error(Guid.Empty, new Exception("Cannot list stores")));
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
    [Fact] void should_not_pass_without_checking_any_store() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_report_the_unavailable_check() => _data.ChecksCouldNotRun.Single(x => x.Check == "Event stores").Check.ShouldEqual("Event stores");
    [Fact] void should_preserve_the_reason() => _data.ChecksCouldNotRun.Single(x => x.Check == "Event stores").Reason.ShouldContain("Cannot list stores");
    [Fact] void should_not_claim_to_have_checked_any_scope() => _data.Scopes.ShouldBeEmpty();
}
