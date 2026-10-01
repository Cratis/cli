// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_there_are_many_scope_tails : given.captured_reports
{
    void Establish() => _data = _data with
    {
        IsAggregate = true,
        EventSequenceTail = null,
        Scopes = [.. Enumerable.Range(1, 20).Select(x => _data with { EventStore = $"store-{x}", Namespace = $"tenant-{x}", EventSequenceTail = (ulong)(100 + x) })]
    };

    void Because() => CaptureReports();

    [Fact] void should_describe_the_aggregate_watch_tail_as_per_scope() => _outputs["watch"].ShouldContain("per scope (see below)");
    [Fact] void should_not_describe_nonempty_scope_tails_as_empty_in_watch() => _outputs["watch"].ShouldNotContain("empty");
    [Fact] void should_describe_the_aggregate_tail_as_per_scope() => _outputs[OutputFormats.Table].ShouldContain("per scope (see below)");
    [Fact] void should_not_describe_completed_tails_as_unavailable() => _outputs[OutputFormats.Table].ShouldNotContain("unavailable");
    [Fact] void should_show_the_first_three_scope_tails() => Enumerable.Range(1, 3).All(x => _outputs["watch"].Contains($"tail: {100 + x}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_show_the_remaining_scope_tails() => Enumerable.Range(4, 17).Any(x => _outputs["watch"].Contains($"tail: {100 + x}", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_show_the_event_store_and_namespace_for_each_visible_tail() => Enumerable.Range(1, 3).All(x => _outputs["watch"].Contains($"store-{x}/tenant-{x}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_direct_to_the_full_report() => _outputs["watch"].ShouldContain("+17 more → rerun without --watch for all tails");
    [Fact] void should_show_one_overflow_row() => WatchIcons("Event sequence tails").Count().ShouldEqual(1);
    [Fact] void should_render_all_scope_tails_in_plain_output() => Enumerable.Range(1, 20).All(x => _outputs[OutputFormats.Plain].Contains($"scope_event_sequence_tail={100 + x} event_store=store-{x} namespace=tenant-{x}", StringComparison.Ordinal)).ShouldBeTrue();
}
