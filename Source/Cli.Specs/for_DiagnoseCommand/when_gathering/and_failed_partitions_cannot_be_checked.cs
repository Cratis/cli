// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_failed_partitions_cannot_be_checked : given.healthy_services
{
    void Establish() => _services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>())
        .Returns(Task.FromException<IEnumerable<FailedPartition>>(new Exception("Permission denied")));

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_not_mark_the_single_scope_report_as_aggregated() => _data.IsAggregate.ShouldBeFalse();
    [Fact] void should_keep_the_selected_scopes_tail() => _data.EventSequenceTail.ShouldEqual((ulong?)10);
    [Fact] void should_not_duplicate_the_successful_tail_in_text() => _outputs[OutputFormats.Table].ShouldNotContain("store/tenant-one: event sequence tail:");
    [Fact] void should_not_add_a_scope_tail_row_in_watch() => WatchIcons("store/tenant-one").ShouldBeEmpty();
    [Fact] void should_not_duplicate_the_successful_tail_in_plain() => _outputs[OutputFormats.Plain].ShouldNotContain("scope_event_sequence_tail=");
    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_report_an_incomplete_check() => _data.ChecksComplete.ShouldBeFalse();
    [Fact] void should_preserve_the_reason_and_scope() => _data.ChecksCouldNotRun.ShouldContainOnly(new DiagnoseCheckFailure("Failed partitions", "store", "tenant-one", "Permission denied"));
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_not_claim_to_have_found_failures() => _data.Findings.ShouldBeEmpty();
}
