// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_the_event_log_is_empty : given.healthy_services
{
    void Establish() => _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(
        QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new EventSequenceTailResponse { SequenceNumber = ulong.MaxValue }));

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_keep_a_completed_empty_sweep_healthy() => _data.IsHealthy.ShouldBeTrue();
    [Fact] void should_describe_the_text_tail_as_empty() => _outputs[OutputFormats.Table].ShouldContain("Event sequence         empty");
    [Fact] void should_describe_the_watch_tail_as_empty() => _outputs["watch"].ShouldContain("empty");
    [Fact] void should_not_describe_the_text_tail_as_unavailable() => _outputs[OutputFormats.Table].ShouldNotContain("unavailable");
    [Fact] void should_not_show_a_failed_watch_tail_check() => WatchIcons("Event sequence tail").Single().Text.ShouldEqual("·");
}
