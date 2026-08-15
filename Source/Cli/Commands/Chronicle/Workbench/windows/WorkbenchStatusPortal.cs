// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI;
using SharpConsoleUI.Builders;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Core;
using SharpConsoleUI.Drawing;
using SharpConsoleUI.Events;
using SharpConsoleUI.Helpers;
using SharpConsoleUI.Layout;
using SharpConsoleUI.Rendering;
using SColor = SharpConsoleUI.Color;
using SRectangle = System.Drawing.Rectangle;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// Hosts the small chooser overlays opened from the status bar — event store, namespace, theme and
/// refresh interval. Each is a desktop portal anchored just above the status bar, opening upward
/// from the hint that spawned it.
/// </summary>
/// <remarks>
/// <para>
/// Only one of these is open at a time, and opening the one that is already open closes it, so a
/// second click on the same hint dismisses rather than reopens.
/// </para>
/// <para>
/// Mouse events arrive on the driver thread. Removing a portal is structural — it restores buffer
/// regions and changes the active window — so it is marshalled onto the UI thread rather than left
/// to race the render loop.
/// </para>
/// </remarks>
public static class WorkbenchStatusPortal
{
    /// <summary>
    /// How long a dismissal suppresses a reopen from the same hint. A click on a hint whose portal is
    /// open dismisses it via click-outside on the press, and without this the release would reopen it.
    /// </summary>
    const int ReopenSuppressMilliseconds = 250;

    /// <summary>Rows of chrome around the list: border top and bottom, plus the list's title row.</summary>
    const int ChromeRows = 3;

    /// <summary>Minimum width so a short label still reads as a panel rather than a sliver.</summary>
    const int MinWidth = 18;

    /// <summary>Columns of padding around the widest entry: border, marker and breathing room.</summary>
    const int WidthPadding = 4;

    /// <summary>Left edge of the chooser, aligned under the status bar rather than centered.</summary>
    const int AnchorX = 2;

    static DesktopPortal? _open;
    static object? _openKey;
    static object? _lastDismissedKey;
    static DateTime _lastDismissed = DateTime.MinValue;

    /// <summary>
    /// Opens a chooser listing <paramref name="items"/>, or closes it when the same chooser is
    /// already open. Selecting an entry invokes <paramref name="onChosen"/> and closes the portal.
    /// </summary>
    /// <param name="windowSystem">The window system hosting the portal.</param>
    /// <param name="theme">The workbench theme, for chrome colors.</param>
    /// <param name="key">Identifies the chooser, so a second click on the same hint toggles it.</param>
    /// <param name="title">Title shown on the list.</param>
    /// <param name="items">Entries to choose from, each a display label and the value it selects.</param>
    /// <param name="current">The value to pre-select, if it is present.</param>
    /// <param name="onChosen">Invoked with the chosen value.</param>
    /// <param name="anchor">
    /// Screen column the chooser lines up with — the edge of the status-bar segment that opened it,
    /// so the panel sits over its own hint. Null falls back to the left margin.
    /// </param>
    /// <param name="alignLeft">
    /// Aligns the panel's left edge to <paramref name="anchor"/> instead of its right. Left-hand
    /// hints anchor by their left edge; right-aligned segments anchor by their right.
    /// </param>
    public static void Open(
        ConsoleWindowSystem windowSystem,
        WorkbenchTheme theme,
        object key,
        string title,
        IReadOnlyList<(string Label, string Value)> items,
        string? current,
        Action<string> onChosen,
        int? anchor = null,
        bool alignLeft = false)
    {
        // Toggle: the same chooser opening again closes it.
        if (_open is not null && ReferenceEquals(_openKey, key))
        {
            Close(windowSystem);
            return;
        }

        // A click on a hint whose portal is open already dismissed it on the press; ignore the
        // release so it closes rather than close-then-reopen.
        if (ReferenceEquals(_lastDismissedKey, key) &&
            (DateTime.UtcNow - _lastDismissed).TotalMilliseconds < ReopenSuppressMilliseconds)
        {
            return;
        }

        CloseAll(windowSystem);

        if (items.Count == 0)
        {
            return;
        }

        var listBuilder = Controls.List(title)
            .Selectable()
            .WithAlignment(SharpConsoleUI.Layout.HorizontalAlignment.Stretch)
            .WithVerticalAlignment(SharpConsoleUI.Layout.VerticalAlignment.Fill);

        foreach (var (label, value) in items)
        {
            listBuilder.AddItem(label, tag: value);
        }

        var list = listBuilder.Build();

        var currentIndex = current is null
            ? -1
            : items.ToList().FindIndex(i => string.Equals(i.Value, current, StringComparison.Ordinal));
        if (currentIndex >= 0)
        {
            list.SelectedIndex = currentIndex;
        }

        // Measure what renders, not what is written: the theme rows carry colour markup around
        // their swatches, and counting the tags made the panel several times wider than its content.
        var widest = items.Max(i => Markup.Remove(i.Label).Length);
        var width = Math.Max(MinWidth, widest + WidthPadding);
        var height = items.Count + ChromeRows;

        // Anchor upward with a row of clearance, so the chooser floats above the status bar rather
        // than sitting flush against it.
        var y = Math.Max(0, windowSystem.DesktopBottomRight.Y - height - 1);

        // Line the panel's right edge up with the segment that opened it, clamped so it never runs
        // off either side of the screen.
        var desiredX = AnchorX;
        if (anchor is { } edge)
        {
            desiredX = alignLeft ? edge : edge - width + 1;
        }
        var x = Math.Max(0, Math.Min(desiredX, windowSystem.DesktopBottomRight.X - width));
        var bounds = new SRectangle(x, y, width, height);

        var content = new WorkbenchPortalContent(list, bounds, theme);

        // ItemActivated fires on the driver's mouse thread for a click. RemovePortal does structural
        // teardown, so the whole handler is marshalled onto the UI thread.
        list.ItemActivated += (_, item) =>
        {
            var portal = _open;
            _open = null;
            _openKey = null;

            windowSystem.EnqueueOnUIThread(() =>
            {
                if (portal is not null)
                {
                    windowSystem.DesktopPortalService.RemovePortal(portal);
                }

                if (item?.Tag is string value)
                {
                    onChosen(value);
                }
            });
        };

        _openKey = key;
        _open = windowSystem.DesktopPortalService.CreatePortal(new DesktopPortalOptions(
            Content: content,
            Bounds: bounds,
            DismissOnClickOutside: true,
            OnDismiss: () =>
            {
                _open = null;
                _lastDismissedKey = _openKey;
                _lastDismissed = DateTime.UtcNow;
                _openKey = null;
            }));
    }

    /// <summary>
    /// Closes any open status-bar chooser. Safe to call when none is open.
    /// </summary>
    /// <param name="windowSystem">The window system hosting the portal.</param>
    public static void CloseAll(ConsoleWindowSystem windowSystem) => Close(windowSystem);

    static void Close(ConsoleWindowSystem windowSystem)
    {
        if (_open is null)
        {
            return;
        }

        var portal = _open;
        _open = null;
        _lastDismissedKey = _openKey;
        _lastDismissed = DateTime.UtcNow;
        _openKey = null;

        windowSystem.EnqueueOnUIThread(() => windowSystem.DesktopPortalService.RemovePortal(portal));
    }
}

/// <summary>
/// Draws the rounded, themed border around a status-bar chooser and hosts the list inside it.
/// </summary>
public sealed class WorkbenchPortalContent : PortalContentBase
{
    readonly SRectangle _bounds;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchPortalContent"/> class.
    /// </summary>
    /// <param name="content">The control hosted inside the border.</param>
    /// <param name="bounds">Absolute screen bounds of the portal.</param>
    /// <param name="theme">The workbench theme, for chrome colors.</param>
    public WorkbenchPortalContent(IWindowControl content, SRectangle bounds, WorkbenchTheme theme)
    {
        _bounds = bounds;

        BorderStyle = BoxChars.Rounded;
        BorderColor = theme.Accent;
        BorderBackgroundColor = theme.Surface;
        BackgroundColor = theme.Surface;
        ForegroundColor = theme.Foreground;
        Content = content;

        // Portals route focus through PortalFocusedControl rather than the window's FocusManager,
        // which is what makes arrow navigation and Enter work inside the portal.
        if (content is IFocusableControl focusable)
        {
            PortalFocusedControl = focusable;
        }
    }

    /// <inheritdoc/>
    public override SRectangle GetPortalBounds() => _bounds;

    /// <summary>
    /// Forwards mouse events to the hosted list so hovering highlights an entry and clicking selects
    /// it; without this the list never sees the mouse and the chooser is keyboard-only.
    /// </summary>
    /// <param name="args">The mouse event.</param>
    /// <returns><see langword="true"/> when the event was consumed.</returns>
    public override bool ProcessMouseEvent(MouseEventArgs args) => ProcessHostedMouseEvent(args);

    /// <summary>
    /// Never called: the list is hosted, so the base paints it directly into the bordered rect.
    /// </summary>
    /// <param name="buffer">The target buffer.</param>
    /// <param name="bounds">The content bounds.</param>
    /// <param name="clipRect">The clip rectangle.</param>
    /// <param name="defaultFg">Default foreground color.</param>
    /// <param name="defaultBg">Default background color.</param>
    protected override void PaintPortalContent(
        CharacterBuffer buffer,
        LayoutRect bounds,
        LayoutRect clipRect,
        SColor defaultFg,
        SColor defaultBg)
    {
    }
}
