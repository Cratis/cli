// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_an_empty_log_and_a_failed_tail_are_discovered : given.healthy_services
{
    void Establish()
    {
        _settings.AllNamespaces = true;
        _services.Sequences.TailSequenceNumber(Arg.Is<TailSequenceNumberRequest>(request => request.Namespace == "tenant-one")).Returns(
            QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new EventSequenceTailResponse { SequenceNumber = ulong.MaxValue }));
        _services.Sequences.TailSequenceNumber(Arg.Is<TailSequenceNumberRequest>(request => request.Namespace == "tenant-two")).Returns(
            QueryResult<EventSequenceTailResponse>.Error(Guid.Empty, new Exception("Cannot read tail")));
    }

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_describe_the_completed_scope_as_empty_in_text() => _outputs[OutputFormats.Table].ShouldContain("store/tenant-one: event sequence tail: empty");
    [Fact] void should_describe_the_failed_scope_as_could_not_check_in_text() => _outputs[OutputFormats.Table].ShouldContain("✗  store/tenant-two: event sequence tail: could not check");
    [Fact] void should_describe_the_completed_scope_as_empty_in_watch() => _outputs["watch"].ShouldContain("tail: empty");
    [Fact] void should_describe_the_failed_scope_as_could_not_check_in_watch() => _outputs["watch"].ShouldContain("tail: could not check");
    [Fact] void should_keep_the_empty_scope_watch_icon_neutral() => WatchIcons("store/tenant-one").Single().Text.ShouldEqual("·");
    [Fact] void should_mark_the_failed_scope_watch_icon_with_a_cross() => WatchIcons("store/tenant-two").Single().Text.ShouldEqual("✗");
    [Fact] void should_color_the_failed_scope_watch_icon_red() => WatchIcons("store/tenant-two").Single().Style.Foreground.ShouldEqual(OutputFormatter.Danger);
    [Fact] void should_not_confuse_either_scope_with_unavailable_text() => _outputs[OutputFormats.Table].ShouldNotContain("unavailable");
    [Fact] void should_return_server_error_for_the_failed_tail() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
