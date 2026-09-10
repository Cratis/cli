// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI;
using SharpConsoleUI.Builders;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Helpers;
using SharpConsoleUI.Layout;
using SharpConsoleUI.Themes;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// Overview tab — server health rail on the left, 2×2 metric tiles (Observers / Failures /
/// Recommendations / Jobs) on the right, and a spanning observer-activity sparkline below the tiles.
/// Uses the WinUI-style <see cref="GridControl"/> for layout (layout B).
/// </summary>
public class OverviewView : IWorkbenchView
{
    /// <summary>
    /// Upper bound on retained samples. The graphs plot one sample per column, so the useful amount
    /// is the panel width — anything beyond that is dropped on render. Sized for a wide terminal and
    /// trimmed to the actual width in <see cref="TrimHistory"/>.
    /// </summary>
    const int HistoryCap = 240;

    /// <summary>
    /// Largest per-tick advance treated as real throughput. Anything beyond this is a re-based tail
    /// (a different event store or namespace), not events that arrived in one refresh interval.
    /// </summary>
    const ulong MaxPlausibleTickDelta = 10_000;

    /// <summary>Name of the observer-count series, needed to target its gradient-carrying series.</summary>
    const string ObserverSeries = "observers";

    /// <summary>Name of the event-throughput series, needed to target its gradient-carrying series.</summary>
    const string ThroughputSeries = "throughput";
    readonly Queue<double> _observerHistory = new(capacity: HistoryCap);
    readonly Queue<double> _eventHistory = new(capacity: HistoryCap);
    ulong? _lastSeenTail;
    ConsoleWindowSystem? _windowSystem;
    WorkbenchTheme? _themeInstance;
    PanelControl? _healthPanel;
    PanelControl? _observersTile;
    PanelControl? _failuresTile;
    PanelControl? _recommendationsTile;
    PanelControl? _jobsTile;
    LineGraphControl? _observerSparkline;
    PanelControl? _activityPanel;
    LineGraphControl? _throughputSparkline;
    PanelControl? _throughputPanel;
    PanelControl? _topTypesPanel;
    GridControl? _grid;
    WorkbenchData? _pendingData;

    /// <inheritdoc/>
    public bool IsActive { get; set; }

    WorkbenchTheme Theme => _themeInstance ??= new WorkbenchTheme(_windowSystem!);

    /// <inheritdoc/>
    public void Dispose()
    {
        _healthPanel?.Dispose();
        _observersTile?.Dispose();
        _failuresTile?.Dispose();
        _recommendationsTile?.Dispose();
        _jobsTile?.Dispose();
        _observerSparkline?.Dispose();
        _activityPanel?.Dispose();
        _throughputSparkline?.Dispose();
        _throughputPanel?.Dispose();
        _topTypesPanel?.Dispose();
        _grid?.Dispose();
    }

    /// <inheritdoc/>
    public void PopulateContent(SharpConsoleUI.Controls.ScrollablePanelControl panel, ConsoleWindowSystem windowSystem)
    {
        // PopulateContent runs on every navigation to this view, so release the previous build first.
        _grid?.Dispose();

        _windowSystem = windowSystem;
        _themeInstance = new WorkbenchTheme(windowSystem);

        // Recolor on the theme's own event rather than on the refresh tick: the tick can be set as
        // high as a minute, which would leave the graphs and the event-type bars on the previous
        // theme's palette until the next poll happened to come round.
        _themeInstance.Changed -= ApplyThemeColors;
        _themeInstance.Changed += ApplyThemeColors;

        // ── Left rail: Server Health panel ─────────────────────────────────────────────────────────
        _healthPanel = Controls.Panel()
            .WithContent("Loading...")
            .WithHeader(" SERVER HEALTH ")
            .Rounded()
            .WithColorRole(ColorRole.Primary)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .WithName("OverviewHealthPanel")
            .Build();

        // ── 2×2 metric tiles ───────────────────────────────────────────────────────────────────────
        _observersTile = Controls.Panel()
            .WithContent("Loading...")
            .WithHeader(" OBSERVERS ")
            .Rounded()
            .WithColorRole(ColorRole.Primary)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .WithName("OverviewObserversTile")
            .Build();

        _failuresTile = Controls.Panel()
            .WithContent("Loading...")
            .WithHeader(" FAILURES ")
            .Rounded()
            .WithColorRole(ColorRole.Primary)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .WithName("OverviewFailuresTile")
            .Build();

        _recommendationsTile = Controls.Panel()
            .WithContent("Loading...")
            .WithHeader(" RECOMMENDATIONS ")
            .Rounded()
            .WithColorRole(ColorRole.Primary)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .WithName("OverviewRecommendationsTile")
            .Build();

        _jobsTile = Controls.Panel()
            .WithContent("Loading...")
            .WithHeader(" JOBS ")
            .Rounded()
            .WithColorRole(ColorRole.Primary)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .WithName("OverviewJobsTile")
            .Build();

        // ── Observer activity line graph, boxed in a panel to match the tiles ──────────────────────
        // A line graph rather than bars: braille cells carry 2x4 sub-pixels, so a delta of 3 versus 4
        // is visible where block bars round both to the same full-height column. AutoFitDataPoints
        // spreads the series across the panel width and MinValue(0) anchors the baseline.
        _observerSparkline = new LineGraphBuilder()
            .WithMode(LineGraphMode.Braille)
            .AddSeries(ObserverSeries, Theme.Accent, ColorGradient.FromColors(Theme.DimAccent, Theme.Accent))
            .WithColorRole(ColorRole.Primary)
            .WithAutoFitDataPoints()
            .WithMinValue(0)
            .WithYAxisLabels(true, "0")
            .WithBaseline(true)
            .WithHighLowLabels(true)

            // Explicitly transparent, not merely unset: an unset background resolves the container's
            // solid color and paints an opaque block over the window gradient.
            .WithBackgroundColor(SharpConsoleUI.Color.Transparent)
            .Stretch()
            .WithVerticalAlignment(SharpConsoleUI.Layout.VerticalAlignment.Fill)
            .Build();

        // Top padding gives the bars breathing room below the header.
        _activityPanel = Controls.Panel()
            .WithHeader(" OBSERVERS OVER TIME ")
            .Rounded()
            .WithColorRole(ColorRole.Primary)
            .WithPadding(1, 1, 1, 0)
            .FillVertical()
            .AddControl(_observerSparkline)
            .WithName("OverviewActivityPanel")
            .Build();

        // ── Event throughput line graph (new events per refresh tick) ──────────────────────────────
        _throughputSparkline = new LineGraphBuilder()
            .WithMode(LineGraphMode.Braille)
            .AddSeries(ThroughputSeries, Theme.Teal, ColorGradient.FromColors(Theme.DimAccent, Theme.Teal))
            .WithColorRole(ColorRole.Info)
            .WithAutoFitDataPoints()
            .WithMinValue(0)
            .WithYAxisLabels(true, "0")
            .WithBaseline(true)
            .WithHighLowLabels(true)

            // Explicitly transparent, not merely unset: an unset background resolves the container's
            // solid color and paints an opaque block over the window gradient.
            .WithBackgroundColor(SharpConsoleUI.Color.Transparent)
            .Stretch()
            .WithVerticalAlignment(SharpConsoleUI.Layout.VerticalAlignment.Fill)
            .Build();

        _throughputPanel = Controls.Panel()
            .WithHeader(" EVENT THROUGHPUT ")
            .Rounded()
            .WithColorRole(ColorRole.Info)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .AddControl(_throughputSparkline)
            .WithName("OverviewThroughputPanel")
            .Build();

        // ── Top event types bar list (from RecentEvents) ───────────────────────────────────────────
        _topTypesPanel = Controls.Panel()
            .WithContent("Loading...")
            .WithHeader(" TOP EVENT TYPES ")
            .Rounded()
            .WithColorRole(ColorRole.Info)
            .WithPadding(1, 0, 1, 0)
            .FillVertical()
            .WithName("OverviewTopTypesPanel")
            .Build();

        // ── Dashboard grid (single GridControl, added directly to the panel) ───────────────────────
        // Columns: health rail | tile/graph A | tile/graph B. Rows: two tile rows, an activity row,
        // and a graph row. The health rail spans all four rows on the left.
        // Rows: two tile rows, an activity row, a graph row — all fixed height — then a trailing Star
        // row that absorbs the remaining height so the dashboard stays compact and top-aligned. The
        // health rail spans the four content rows on the left.
        _grid = Controls.Grid()
            .Columns(GridLength.Star(1), GridLength.Star(1), GridLength.Star(1))

            // The tile rows are fixed — they hold a couple of lines each and gain nothing from being
            // taller. The two graph rows take the remaining height instead of a trailing spacer, so a
            // tall terminal grows the sparklines rather than leaving a band of empty cells.
            .Rows(GridLength.Cells(7), GridLength.Cells(7), GridLength.Star(1), GridLength.Star(1))
            .RowGap(1)
            .ColumnGap(1)
            .WithColorRole(ColorRole.Primary)
            .WithAlignment(SharpConsoleUI.Layout.HorizontalAlignment.Stretch)
            .WithVerticalAlignment(SharpConsoleUI.Layout.VerticalAlignment.Fill)
            .ColumnSplitterAfter(0)
            .Place(_healthPanel, 0, 0, rowSpan: 4)
            .Place(_observersTile, 0, 1)
            .Place(_failuresTile, 0, 2)
            .Place(_recommendationsTile, 1, 1)
            .Place(_jobsTile, 1, 2)

            // Throughput takes the full-width row: it is the series that actually moves, and the
            // extra columns are more history. The observer count is a flat line most of the time,
            // so it sits in the narrower row beside the event-type breakdown.
            .Place(_throughputPanel, 2, 1, colSpan: 2)
            .Place(_activityPanel, 3, 1)
            .Place(_topTypesPanel, 3, 2)
            .Build();

        if (_pendingData is not null)
        {
            UpdateData(_pendingData);
        }

        panel.ClearContents();
        panel.AddControl(_grid);
    }

    /// <inheritdoc/>
    public void UpdateData(WorkbenchData data)
    {
        _pendingData = data;
        if (_healthPanel is null) return;

        var suc = Theme.Success.ToMarkup();
        var dan = Theme.Danger.ToMarkup();
        var mut = Theme.Muted.ToMarkup();
        var war = Theme.Warning.ToMarkup();
        var acc = Theme.Accent.ToMarkup();

        // ── Server Health rail ─────────────────────────────────────────────────────────────────────
        var connStatus = data.IsConnected
            ? $"[{suc}]● Connected[/]"
            : $"[{dan}]○ Disconnected[/]";
        var version = data.ServerVersion is not null
            ? $"[{mut}]v{data.ServerVersion}[/]"
            : $"[{mut}]unknown[/]";
        var seq = data.TailSequenceNumber.HasValue
            ? $"[bold]#{data.TailSequenceNumber.Value:N0}[/]"
            : $"[{mut}]—[/]";

        _healthPanel.Content =
            $"{connStatus}   [{mut}]{version}[/]\n" +
            "\n" +
            $"[{acc}][bold]CONTEXT[/][/]\n" +
            $"  [{mut}]Store[/]      [bold]{data.EventStore}[/]\n" +
            $"  [{mut}]Namespace[/]  [bold]{data.Namespace}[/]\n" +
            $"  [{mut}]Tail seq[/]   {seq}\n" +
            "\n" +
            $"[{acc}][bold]CATALOG[/][/]\n" +
            $"  [bold]{data.EventTypeRegistrations.Count,4}[/] [{mut}]event types[/]\n" +
            $"  [bold]{data.ProjectionDefinitions.Count,4}[/] [{mut}]projections[/]\n" +
            $"  [bold]{data.ReadModelDefinitions.Count,4}[/] [{mut}]read models[/]\n" +
            $"  [bold]{data.EventStoreSubscriptions.Count,4}[/] [{mut}]subscriptions[/]\n" +
            "\n" +
            $"[{mut}]{data.ConnectionString}[/]\n" +
            "\n" +
            $"[{mut}]⟳ updated {FormatAge(data.CapturedAt)}[/]";

        // ── Observers tile ─────────────────────────────────────────────────────────────────────────
        ColorRole observersRole;
        if (data.DisconnectedObservers > 0) observersRole = ColorRole.Danger;
        else if (data.ReplayingObservers > 0) observersRole = ColorRole.Warning;
        else observersRole = ColorRole.Primary;
        _observersTile!.ColorRole = observersRole;

        string obsColor;
        if (data.DisconnectedObservers > 0) obsColor = dan;
        else if (data.ReplayingObservers > 0) obsColor = war;
        else obsColor = suc;

        // Hero number (the total) on the headline row, then a compact, color-coded state breakdown.
        // Zero-count states are dimmed so the eye lands on what is actually present.
        string ObsStat(string glyph, string color, int n, string label) =>
            n > 0
                ? $"[{color}]{glyph}[/] [bold]{n}[/] [{mut}]{label}[/]"
                : $"[{mut}]{glyph} {n} {label}[/]";

        _observersTile.Content =
            $"[{obsColor}][bold]{data.Observers.Count}[/][/] [{mut}]observers[/]\n" +
            "\n" +
            $"{ObsStat("●", suc, data.ActiveObservers, "active")}   {ObsStat("▲", war, data.ReplayingObservers, "replaying")}\n" +
            $"{ObsStat("○", mut, data.SuspendedObservers, "suspended")}   {ObsStat("⊘", dan, data.DisconnectedObservers, "disconnected")}";

        // ── Failures tile ──────────────────────────────────────────────────────────────────────────
        // Hero stat: a big green ✓ when healthy, a big danger count + call-to-action when not.
        _failuresTile!.ColorRole = data.FailedPartitions.Count > 0 ? ColorRole.Danger : ColorRole.Primary;
        _failuresTile.Content = data.FailedPartitions.Count > 0
            ? $"[{dan}][bold]{data.FailedPartitions.Count}[/][/] [{mut}]failed partition{(data.FailedPartitions.Count == 1 ? string.Empty : "s")}[/]\n\n[{dan}]⚠[/] [{mut}]needs attention[/]   [{acc}]press 3[/]"
            : $"[{suc}][bold]✓[/][/] [{mut}]all partitions healthy[/]";

        // ── Recommendations tile ───────────────────────────────────────────────────────────────────
        _recommendationsTile!.ColorRole = data.Recommendations.Count > 0 ? ColorRole.Warning : ColorRole.Primary;
        _recommendationsTile.Content = data.Recommendations.Count > 0
            ? $"[{war}][bold]{data.Recommendations.Count}[/][/] [{mut}]pending recommendation{(data.Recommendations.Count == 1 ? string.Empty : "s")}[/]\n\n[{war}]![/] [{mut}]review suggested[/]   [{acc}]press 5[/]"
            : $"[{suc}][bold]✓[/][/] [{mut}]no recommendations[/]";

        // ── Jobs tile ──────────────────────────────────────────────────────────────────────────────
        _jobsTile!.ColorRole = ColorRole.Primary;
        _jobsTile.Content = data.Jobs.Count > 0
            ? $"[{acc}][bold]{data.Jobs.Count}[/][/] [{mut}]job{(data.Jobs.Count == 1 ? string.Empty : "s")} running[/]"
            : $"[{suc}][bold]✓[/][/] [{mut}]no jobs running[/]";

        // ── Graphs (observers over time, event throughput, top event types) ────────────────────────
        UpdateObserverSparkline(data.Observers.Count);
        UpdateThroughput(data.TailSequenceNumber);
        UpdateTopTypes(data, mut, Theme.Teal.ToMarkup());
    }

    /// <summary>
    /// Formats how long ago a snapshot was captured, relative to now.
    /// </summary>
    /// <param name="capturedAt">When the snapshot was captured.</param>
    /// <returns>A short relative-time label such as "just now" or "12s ago".</returns>
    static string FormatAge(DateTimeOffset capturedAt)
    {
        var seconds = (int)Math.Max(0, (DateTimeOffset.Now - capturedAt).TotalSeconds);
        return seconds <= 1 ? "just now" : $"{seconds}s ago";
    }

    /// <summary>
    /// Drops samples that have scrolled off the left edge. A graph plots one sample per column, so
    /// retaining more than the panel can show would silently discard the newest points.
    /// </summary>
    /// <param name="history">The series to trim.</param>
    static void TrimHistory(Queue<double> history)
    {
        var visible = Math.Clamp(Console.WindowWidth / 2, 16, HistoryCap);
        while (history.Count > visible)
        {
            history.Dequeue();
        }
    }

    /// <summary>
    /// Returns the value at the given percentile of a series, used to scale a graph so that a lone
    /// outlier cannot flatten every ordinary sample against the baseline.
    /// </summary>
    /// <param name="values">The samples to examine.</param>
    /// <param name="percentile">The percentile to take, between 0 and 1.</param>
    /// <returns>The value at that percentile, or 0 for an empty series.</returns>
    static double Percentile(Queue<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var ordered = values.Order().ToList();
        var index = Math.Clamp((int)Math.Ceiling(percentile * ordered.Count) - 1, 0, ordered.Count - 1);

        return ordered[index];
    }

    void UpdateObserverSparkline(int totalObservers)
    {
        if (_observerSparkline is null) return;

        _observerHistory.Enqueue(totalObservers);
        TrimHistory(_observerHistory);

        // Give the series ~25% headroom above its peak so a steady value reads as a mid-height flat
        // line (not a solid full-height block) and a rise has room to climb into.
        var peak = _observerHistory.Count > 0 ? _observerHistory.Max() : 0;
        _observerSparkline.MaxValue = Math.Max(1, Math.Ceiling(peak * 1.25));
        _observerSparkline.SetDataPoints(ObserverSeries, _observerHistory);
    }

    /// <summary>
    /// Tracks new events per refresh tick (tail-sequence delta) and feeds the throughput sparkline.
    /// </summary>
    /// <param name="tail">The current tail sequence number, or null when unavailable.</param>
    void UpdateThroughput(ulong? tail)
    {
        if (_throughputSparkline is null) return;

        double delta = 0;
        if (tail.HasValue)
        {
            if (_lastSeenTail.HasValue && tail.Value >= _lastSeenTail.Value)
            {
                var advance = tail.Value - _lastSeenTail.Value;

                // A jump far beyond what a single tick could produce means the tail belongs to a
                // different sequence — switching event store or namespace re-bases it. Recording that
                // as one sample would put a spike of thousands next to deltas of single digits, and
                // the scale it forces flattens every real value onto the baseline until it scrolls
                // off. Start a fresh series instead.
                if (advance > MaxPlausibleTickDelta)
                {
                    _eventHistory.Clear();
                }
                else
                {
                    delta = advance;
                }
            }

            _lastSeenTail = tail.Value;
        }

        _eventHistory.Enqueue(delta);
        TrimHistory(_eventHistory);

        // Scale to the 90th percentile rather than the maximum. A single outlier — the first sample
        // after a re-based tail, or a burst — would otherwise set a ceiling thousands of times the
        // normal rate, flattening every ordinary value onto the baseline until it scrolls off. The
        // outlier still draws, clipped at the top, where it reads as "off the chart".
        _throughputSparkline.MaxValue = Math.Max(1, Math.Ceiling(Percentile(_eventHistory, 0.9) * 1.25));
        _throughputSparkline.SetDataPoints(ThroughputSeries, _eventHistory);

        // The sparkline is blank when every recent delta is 0, which reads as "broken". Reflect the
        // idle/active state in the panel header so the empty box reads as intentional.
        if (_throughputPanel is { } panel)
        {
            panel.Header = _eventHistory.Any(v => v > 0)
                ? $" EVENT THROUGHPUT · +{(long)delta}/tick "
                : " EVENT THROUGHPUT · idle ";
        }
    }

    /// <summary>
    /// Renders the most frequent event types from the recent-events window as a horizontal bar list.
    /// </summary>
    /// <param name="data">The current snapshot.</param>
    /// <param name="mut">Muted color markup.</param>
    /// <param name="accent">Accent color markup for the type names.</param>
    void UpdateTopTypes(WorkbenchData data, string mut, string accent)
    {
        if (_topTypesPanel is null) return;

        var counts = data.RecentEvents
            .GroupBy(e => e.Context.EventType.Id)
            .Select(g => (Type: g.Key, Count: g.Count()))
            .OrderByDescending(t => t.Count)
            .Take(5)
            .ToList();

        if (counts.Count == 0)
        {
            _topTypesPanel.Content = $"[{mut}]No recent events[/]";
            return;
        }

        var max = counts[0].Count;
        var lines = counts.Select(t =>
        {
            var name = t.Type.Length > 22 ? t.Type[..21] + "…" : t.Type;
            return $"[{accent}]{name,-22}[/] {WorkbenchUi.GradientBar(t.Count, max, 12, Theme.Teal, Theme.Accent, Theme.Muted)} [{mut}]{t.Count}[/]";
        });

        _topTypesPanel.Content = string.Join('\n', lines);
    }

    /// <summary>
    /// Re-resolves the colors that are baked into built controls — the graph series and their
    /// gradients — and repaints the content that carries theme-colored markup.
    /// </summary>
    void ApplyThemeColors()
    {
        if (_observerSparkline?.Series.FirstOrDefault(s => s.Name == ObserverSeries) is { } observers)
        {
            observers.LineColor = Theme.Accent;
            observers.Gradient = ColorGradient.FromColors(Theme.DimAccent, Theme.Accent);
        }

        if (_throughputSparkline?.Series.FirstOrDefault(s => s.Name == ThroughputSeries) is { } throughput)
        {
            throughput.LineColor = Theme.Teal;
            throughput.Gradient = ColorGradient.FromColors(Theme.DimAccent, Theme.Teal);
        }

        // The tiles and the event-type bars render theme colors as markup, so they only pick up a
        // new palette when their content is rebuilt.
        if (_pendingData is not null)
        {
            UpdateData(_pendingData);
        }
    }
}
