// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_only_one_namespace_is_discovered : given.healthy_services
{
    void Establish()
    {
        _settings.AllNamespaces = true;
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>()).Returns(
            QueryResult<IEnumerable<NamespaceNamesResponse>>.Success(Guid.Empty, [new NamespaceNamesResponse { Name = "tenant-one" }]));
    }

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_mark_the_report_as_aggregated() => _data.IsAggregate.ShouldBeTrue();
    [Fact] void should_not_report_a_top_level_tail() => _data.EventSequenceTail.ShouldBeNull();
    [Fact] void should_omit_the_null_top_level_json_tail() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.TryGetProperty("eventSequenceTail", out _).ShouldBeFalse();
    [Fact] void should_preserve_the_scopes_json_tail() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("scopes")[0].GetProperty("eventSequenceTail").GetUInt64().ShouldEqual(10UL);
    [Fact] void should_leave_the_top_level_plain_tail_empty() => _outputs[OutputFormats.Plain].Split(Environment.NewLine).Single(line => line.StartsWith("event_sequence_tail=", StringComparison.Ordinal)).ShouldEqual("event_sequence_tail=");
    [Fact] void should_preserve_the_scopes_plain_tail() => _outputs[OutputFormats.Plain].ShouldContain("scope_event_sequence_tail=10 event_store=store namespace=tenant-one");
    [Fact] void should_point_to_the_scoped_tail_in_text() => _outputs[OutputFormats.Table].ShouldContain("per scope (see below)");
    [Fact] void should_show_the_scopes_tail_in_text() => _outputs[OutputFormats.Table].ShouldContain("store/tenant-one: event sequence tail: 10");
    [Fact] void should_point_to_the_scoped_tail_in_watch() => _outputs["watch"].ShouldContain("per scope (see below)");
    [Fact] void should_show_the_scopes_tail_in_watch() => _outputs["watch"].ShouldContain("tail: 10");
    [Fact] void should_keep_the_complete_report_healthy() => _data.IsHealthy.ShouldBeTrue();
}
