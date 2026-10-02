// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Cli.Commands.Chronicle.Workbench;
using SharpConsoleUI;
using SharpConsoleUI.Builders;
using SharpConsoleUI.Drivers;

namespace Cratis.Cli.for_FailedPartitionsView.when_rendering_the_detail;

/// <summary>
/// Verifies the detail panel orders the attempts by their timestamp, newest first, instead of asking an
/// object without a comparer to sort (which crashed the workbench on any partition retried more than once).
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class and_a_partition_has_several_attempts : Specification
{
    static readonly DateTimeOffset _first = new(2026, 9, 30, 18, 0, 0, TimeSpan.Zero);

    TestFailedPartitionsView _view;
    FailedPartition _partition;
    string _detail;

    void Establish()
    {
        var windowSystem = new ConsoleWindowSystem(new HeadlessConsoleDriver(200, 50));
        _view = new TestFailedPartitionsView();
        _view.PopulateContent(Controls.ScrollablePanel().Build(), windowSystem);

        // Stored out of order and with different offsets, as a server returns them.
        _partition = new FailedPartition
        {
            ObserverId = "an-observer",
            Partition = "a-partition",
            Attempts =
            [
                Attempt(_first.AddSeconds(2), "second"),
                Attempt(_first, "first"),
                Attempt(_first.AddSeconds(8).ToOffset(TimeSpan.FromHours(5)), "fourth"),
                Attempt(_first.AddSeconds(4), "third"),
                Attempt(_first.AddSeconds(16), "fifth"),
                Attempt(_first.AddSeconds(32), "sixth")
            ]
        };
    }

    void Because() => _detail = _view.Detail(_partition);

    [Fact] void should_render_the_newest_attempt_first() =>
        _detail.IndexOf("sixth", StringComparison.Ordinal).ShouldBeLessThan(_detail.IndexOf("fifth", StringComparison.Ordinal));

    [Fact] void should_keep_the_attempts_in_time_order() =>
        _detail.IndexOf("fifth", StringComparison.Ordinal).ShouldBeLessThan(_detail.IndexOf("fourth", StringComparison.Ordinal));

    [Fact] void should_compare_across_offsets() =>
        _detail.IndexOf("fourth", StringComparison.Ordinal).ShouldBeLessThan(_detail.IndexOf("third", StringComparison.Ordinal));

    [Fact] void should_show_only_the_five_latest() => _detail.ShouldNotContain("first");

    static FailedPartitionAttempt Attempt(DateTimeOffset occurred, string message) => new()
    {
        Occurred = occurred,
        Messages = [message]
    };

    class TestFailedPartitionsView : FailedPartitionsView
    {
        public string Detail(FailedPartition item) => RenderDetail(item, null);
    }
}
