// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_first_watch_sweep_is_pending : given.captured_reports
{
    void Establish()
    {
        _watchPending = true;
        _data = _data with { ServerReachable = false, EventSequenceTail = null };
    }

    void Because() => CaptureReports();

    [Theory]
    [InlineData("Health")]
    [InlineData("Connection")]
    void should_render_neutral_pending_icons(string label) => WatchIcons(label).Single().Text.ShouldEqual("·");
    [Theory]
    [InlineData("Health")]
    [InlineData("Connection")]
    void should_use_muted_pending_colors(string label) => WatchIcons(label).Single().Style.Foreground.ShouldEqual(OutputFormatter.Muted);
    [Fact] void should_show_checking_for_health_and_connection() => _outputs["watch"].Split("checking…", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_not_claim_issues_before_the_first_sweep() => _outputs["watch"].ShouldNotContain("issues detected");
    [Fact] void should_not_claim_the_connection_is_unreachable_before_the_first_sweep() => _outputs["watch"].ShouldNotContain("unreachable");
}
