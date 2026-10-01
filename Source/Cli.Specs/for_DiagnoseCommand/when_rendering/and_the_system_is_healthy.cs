// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_system_is_healthy : given.captured_reports
{
    void Because() => CaptureReports();

    [Fact] void should_mark_watch_health_with_a_checkmark() => WatchIcons("Health").Single().Text.ShouldEqual("✓");
    [Fact] void should_color_watch_health_green() => WatchIcons("Health").Single().Style.Foreground.ShouldEqual(OutputFormatter.Success);
}
