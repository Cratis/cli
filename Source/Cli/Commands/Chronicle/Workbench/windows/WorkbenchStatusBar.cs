// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI.Builders;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Helpers;
using SharpConsoleUI.Themes;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// Builds and owns the live bottom status bar for the Chronicle workbench.
/// The left zone contains static key hints; the right zone contains live status items
/// (connection, server version, refresh interval, and active context) updated each data tick.
/// </summary>
public class WorkbenchStatusBar
{
    /// <summary>Columns a separator occupies, including the spaces around it.</summary>
    const int SeparatorWidth = 5;

    /// <summary>Columns between the last segment and the right edge of the bar.</summary>
    const int TrailingMargin = 2;

    /// <summary>Columns before the first segment on the left.</summary>
    const int LeftMargin = 1;

    /// <summary>Columns between two segments inside the same cluster.</summary>
    const int ItemGap = 2;

    const int MaxContextLength = 40;

    readonly StatusBarItem _connectionItem;
    readonly StatusBarItem _versionItem;
    readonly StatusBarItem _intervalItem;
    readonly StatusBarItem _quitItem;
    readonly StatusBarItem _paletteItem;
    readonly StatusBarItem _helpItem;
    readonly StatusBarItem _filterItem;
    readonly StatusBarItem _themeItem;
    readonly StatusBarItem _eventStoreItem;
    readonly StatusBarItem _namespaceItem;

    readonly WorkbenchTheme _theme;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchStatusBar"/> class and builds the
    /// <see cref="StatusBarControl"/>. The left key hints are clickable and fire the same action as their
    /// shortcut; the actions are passed as delegates so they can resolve subsystems built later.
    /// </summary>
    /// <param name="theme">The workbench theme, used to color the connection indicator by state.</param>
    /// <param name="onQuit">Invoked when the Quit hint is clicked.</param>
    /// <param name="onPalette">Invoked when the Palette hint is clicked.</param>
    /// <param name="onHelp">Invoked when the Help hint is clicked.</param>
    /// <param name="onFilter">Invoked when the Filter hint is clicked.</param>
    /// <param name="onTheme">Invoked when the Theme hint is clicked.</param>
    /// <param name="onInterval">Invoked when the refresh interval is clicked.</param>
    /// <param name="onEventStore">Invoked when the event store is clicked.</param>
    /// <param name="onNamespace">Invoked when the namespace is clicked.</param>
    public WorkbenchStatusBar(
        WorkbenchTheme theme,
        Action onQuit,
        Action onPalette,
        Action onHelp,
        Action onFilter,
        Action onTheme,
        Action onInterval,
        Action onEventStore,
        Action onNamespace)
    {
        _theme = theme;
        _connectionItem = new StatusBarItem { Label = "● Connecting…" };
        _versionItem = new StatusBarItem { Label = string.Empty };

        // The interval, store and namespace are click targets: each opens a chooser above the status
        // bar. Store and namespace are separate items rather than one "store/namespace" label so each
        // opens its own chooser, divided by the bar's own separator.
        _quitItem = new StatusBarItem { Shortcut = "Q", Label = "Quit", OnClick = onQuit };
        _paletteItem = new StatusBarItem { Shortcut = "Ctrl+P", Label = "Palette", OnClick = onPalette };
        _helpItem = new StatusBarItem { Shortcut = "?", Label = "Help", OnClick = onHelp };
        _filterItem = new StatusBarItem { Shortcut = "F", Label = "Filter", OnClick = onFilter };
        _themeItem = new StatusBarItem { Shortcut = "F9", Label = "Theme", OnClick = onTheme };
        _intervalItem = new StatusBarItem { Label = string.Empty, OnClick = onInterval };
        _eventStoreItem = new StatusBarItem { Label = string.Empty, OnClick = onEventStore };
        _namespaceItem = new StatusBarItem { Label = string.Empty, OnClick = onNamespace };

        ApplyClickableAccent();

        // Recolor on the theme's own change event, not on the refresh tick: the tick can be set as
        // high as a minute, which would leave these segments on the previous theme's accent until
        // the next poll happened to come round.
        theme.Changed += ApplyClickableAccent;

        Control = Controls.StatusBar()
            .AddLeft(_quitItem)
            .AddLeft(_paletteItem)
            .AddLeftSeparator()
            .AddLeft(_helpItem)
            .AddLeft(_filterItem)
            .AddLeftSeparator()
            .AddLeft(_themeItem)
            .AddRight(_connectionItem)
            .AddRightSeparator()
            .AddRight(_versionItem)
            .AddRightSeparator()
            .AddRight(_intervalItem)
            .AddRightSeparator()
            .AddRight(_eventStoreItem)
            .AddRightSeparator()
            .AddRight(_namespaceItem)
            .WithColorRole(ColorRole.Default)
            .WithAboveLine()
            .StickyBottom()
            .Build();
    }

    /// <summary>
    /// Gets the built <see cref="StatusBarControl"/> to be added to the window.
    /// </summary>
    public StatusBarControl Control { get; }

    /// <summary>Right-aligned segments in render order, used to locate a segment's right edge.</summary>
    StatusBarItem[] RightGroup =>
        [_connectionItem, _versionItem, _intervalItem, _eventStoreItem, _namespaceItem];

    /// <summary>
    /// Updates the live right-zone items from the latest workbench snapshot and settings.
    /// Call this on every data tick from <see cref="WorkbenchRefreshLoop"/>.
    /// </summary>
    /// <param name="data">The freshly fetched workbench data snapshot.</param>
    /// <param name="settings">The workbench settings (provides the refresh interval).</param>
    /// <param name="getActiveEventStore">Returns the currently active event store name, or <see langword="null"/> for the default.</param>
    /// <param name="getActiveNamespace">Returns the currently active namespace name, or <see langword="null"/> for the default.</param>
    public void Update(
        WorkbenchData data,
        WorkbenchSettings settings,
        Func<string?> getActiveEventStore,
        Func<string?> getActiveNamespace)
    {
        // Color the connection indicator by state (re-read each tick so it follows theme changes):
        // green/Success when connected, red/Danger when not.
        if (data.IsConnected)
        {
            _connectionItem.Label = "● Connected";
            _connectionItem.LabelForeground = _theme.Success;
        }
        else
        {
            _connectionItem.Label = "○ Disconnected";
            _connectionItem.LabelForeground = _theme.Danger;
        }

        _versionItem.Label = data.ServerVersion is not null ? $"v{data.ServerVersion}" : string.Empty;
        _intervalItem.Label = $"↻ {settings.Interval}s";

        // Value first, shortcut as a dimmed parenthetical — the same shape the view toolbars use
        // ("Filter (F)"), so the reading is "this is the store, and this key changes it".
        _eventStoreItem.Label = WithShortcut(getActiveEventStore() ?? settings.ResolveEventStore(), "Ctrl+E");
        _namespaceItem.Label = WithShortcut(getActiveNamespace() ?? settings.ResolveNamespace(), "Ctrl+N");
    }

    /// <summary>
    /// Screen column of the right edge of the event-store segment, used to anchor its chooser under
    /// the segment that opened it.
    /// </summary>
    /// <returns>The column, or null when the bar has not been laid out yet.</returns>
    public int EventStoreRightEdge() => RightEdge(RightGroup, _eventStoreItem);

    /// <summary>
    /// Screen column of the right edge of the namespace segment.
    /// </summary>
    /// <returns>The column of the segment's right edge.</returns>
    public int NamespaceRightEdge() => RightEdge(RightGroup, _namespaceItem);

    /// <summary>
    /// Screen column of the left edge of the Theme hint. The left group lays out from the left
    /// edge, so this accumulates the widths ahead of it rather than counting back from the right.
    /// </summary>
    /// <returns>The column of the hint's left edge.</returns>
    public int ThemeLeftEdge()
    {
        // Left group in render order, with the separators the bar draws between the three clusters.
        var leading =
            Width(_quitItem) + ItemGap +
            Width(_paletteItem) + SeparatorWidth +
            Width(_helpItem) + ItemGap +
            Width(_filterItem) + SeparatorWidth;

        return LeftMargin + leading;
    }

    /// <summary>
    /// Screen column of the right edge of the refresh-interval segment.
    /// </summary>
    /// <returns>The column of the segment's right edge.</returns>
    public int IntervalRightEdge() => RightEdge(RightGroup, _intervalItem);

    /// <summary>Rendered width of a segment: its shortcut, a space, and its label.</summary>
    /// <param name="item">The segment to measure.</param>
    /// <returns>The width in columns.</returns>
    static int Width(StatusBarItem item) =>
        (string.IsNullOrEmpty(item.Shortcut) ? 0 : item.Shortcut.Length + 1) +
        (item.Label is null ? 0 : Markup.Remove(item.Label).Length);

    /// <summary>
    /// Computes the right edge of <paramref name="item"/> within a right-aligned group.
    /// </summary>
    /// <param name="rightGroup">The group in render order.</param>
    /// <param name="item">The segment to locate.</param>
    /// <returns>The screen column of the segment's right edge.</returns>
    static int RightEdge(StatusBarItem[] rightGroup, StatusBarItem item)
    {
        var index = Array.IndexOf(rightGroup, item);
        if (index < 0)
        {
            return Console.WindowWidth - 1;
        }

        var trailing = 0;
        for (var i = index + 1; i < rightGroup.Length; i++)
        {
            trailing += Width(rightGroup[i]) + SeparatorWidth;
        }

        return Math.Max(0, Console.WindowWidth - TrailingMargin - trailing);
    }

    /// <summary>
    /// Shortens a label that would otherwise crowd the status bar.
    /// </summary>
    /// <param name="value">The label to shorten.</param>
    /// <returns>The label, elided when too long.</returns>
    static string Truncate(string value) =>
        value.Length <= MaxContextLength ? value : value[..(MaxContextLength - 1)] + "…";

    /// <summary>
    /// Renders a clickable value with its shortcut as a dimmed parenthetical.
    /// </summary>
    /// <param name="value">The value shown, such as the event store name.</param>
    /// <param name="shortcut">The key that opens the same chooser.</param>
    /// <returns>The markup for the segment label.</returns>
    string WithShortcut(string value, string shortcut) =>
        $"{Truncate(value)} [{_theme.Muted.ToMarkup()}]({shortcut})[/]";

    /// <summary>
    /// Paints the clickable segments in the accent color so they read as interactive.
    /// </summary>
    void ApplyClickableAccent()
    {
        foreach (var clickable in new[] { _intervalItem, _eventStoreItem, _namespaceItem })
        {
            clickable.LabelForeground = _theme.Accent;
        }
    }
}
