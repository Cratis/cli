// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_event_sequence_could_not_run : given.captured_reports
{
    void Establish() => _data = _data with
    {
        EventSequenceTail = null,
        ChecksCouldNotRun = [new DiagnoseCheckFailure("Event sequence", "store", "tenant-one", "Cannot read tail")]
    };

    void Because() => CaptureReports();

    [Fact] void should_mark_the_text_row_with_a_cross() => _outputs[OutputFormats.Table].ShouldContain("✗  Event sequence");
    [Fact] void should_mark_the_watch_row_with_a_cross() => WatchIcons("Event sequence tail").Single().Text.ShouldEqual("✗");
    [Fact] void should_color_the_watch_row_red() => WatchIcons("Event sequence tail").Single().Style.Foreground.ShouldEqual(OutputFormatter.Danger);
}
