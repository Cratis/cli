// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_there_are_many_unavailable_checks : given.captured_reports
{
    void Establish() => _data = _data with
    {
        ChecksCouldNotRun = [.. Enumerable.Range(1, 20).Select(x => new DiagnoseCheckFailure("Observers", "store", $"tenant-{x}", $"Permission denied [scope-{x}]"))]
    };

    void Because() => CaptureReports();

    [Fact] void should_show_the_first_three_unavailable_checks() => Enumerable.Range(1, 3).All(x => _outputs["watch"].Contains($"Permission denied [scope-{x}]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_show_the_remaining_unavailable_checks() => Enumerable.Range(4, 17).Any(x => _outputs["watch"].Contains($"Permission denied [scope-{x}]", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_show_only_three_checks_and_one_overflow_row() => WatchIcons("Could not check").Count().ShouldEqual(4);
    [Fact] void should_direct_to_the_full_report() => _outputs["watch"].ShouldContain("+17 more → rerun without --watch for all checks");
    [Fact] void should_keep_unavailable_checks_before_other_checks() => _outputs["watch"].IndexOf("for all checks", StringComparison.Ordinal).ShouldBeLessThan(_outputs["watch"].IndexOf("Connection", StringComparison.Ordinal));
    [Fact] void should_mark_each_unavailable_check_with_a_cross() => WatchIcons("Could not check").All(icon => icon.Text == "✗").ShouldBeTrue();
    [Fact] void should_color_each_unavailable_check_red() => WatchIcons("Could not check").All(icon => icon.Style.Foreground == OutputFormatter.Danger).ShouldBeTrue();
    [Fact] void should_render_all_unavailable_checks_in_text() => Enumerable.Range(1, 20).All(x => _outputs[OutputFormats.Table].Contains($"Permission denied [scope-{x}]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_render_all_unavailable_checks_in_plain_output() => Enumerable.Range(1, 20).All(x => _outputs[OutputFormats.Plain].Contains($"Permission denied [scope-{x}]", StringComparison.Ordinal)).ShouldBeTrue();
}
