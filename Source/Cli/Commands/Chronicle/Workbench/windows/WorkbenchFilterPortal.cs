// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI;
using SharpConsoleUI.Builders;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Drawing;
using SharpConsoleUI.Events;
using SharpConsoleUI.Helpers;
using SharpConsoleUI.Themes;
using SRectangle = System.Drawing.Rectangle;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// A floating portal overlay that filters the current view's table as the user types. Anchored near
/// the top of the window (row 1), horizontally centered. Hosts a search prompt and a live result
/// caption reporting how many rows match.
/// </summary>
/// <remarks>
/// <para>
/// The filter intercepts all keystrokes while open; the hosting window must forward
/// <c>PreviewKeyPressed</c> events to <see cref="ProcessKey"/> and set
/// <see cref="KeyPressedEventArgs.Handled"/> to <see langword="true"/> so keys never reach the table
/// beneath — otherwise typing would move the table selection instead of editing the filter.
/// </para>
/// <para>
/// Every keystroke raises <see cref="TextChanged"/> so the view re-filters live. Enter raises
/// <see cref="Committed"/> to close the portal while keeping the filter applied; Esc raises
/// <see cref="Cancelled"/>, which restores the filter the view had when the portal opened.
/// Framework-initiated dismissals (outside-click) fire the base
/// <see cref="PortalContentBase.DismissRequested"/> event.
/// </para>
/// </remarks>
public sealed class WorkbenchFilterPortal : PortalContentContainer
{
    const int FilterMaxWidth = 70;

    /// <summary>Border (2) + header (1) + prompt (1) + rule (1) + caption (1).</summary>
    const int FilterHeight = 6;

    readonly PromptControl _searchInput;
    readonly MarkupControl _resultCaption;
    readonly string _mutedMarkup;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchFilterPortal"/> class.
    /// </summary>
    /// <param name="initialFilter">The filter currently applied to the view, shown as the starting text.</param>
    /// <param name="title">The view name, shown in the portal header so it is clear what is being filtered.</param>
    /// <param name="theme">The active workbench theme — used for chrome colors so the portal follows F9/F10/F11.</param>
    /// <param name="windowWidth">The current window width in columns, used to position and size the portal.</param>
    /// <param name="windowHeight">The current window height in rows, used to clamp the portal vertically.</param>
    public WorkbenchFilterPortal(
        string initialFilter,
        string title,
        WorkbenchTheme theme,
        int windowWidth,
        int windowHeight)
    {
        _mutedMarkup = theme.Muted.ToMarkup();

        DismissOnOutsideClick = true;
        BorderStyle = BoxChars.Rounded;

        // A heavily-dimmed accent so the chrome stays quiet and the typed text holds the eye —
        // matching the command palette, which anchors in the same place.
        var dimChrome = theme.Accent.Mix(theme.Background, 0.8);
        BorderColor = dimChrome;
        BorderBackgroundColor = theme.Surface;
        BackgroundColor = theme.Surface;
        ForegroundColor = theme.Foreground;

        // PortalContentContainer has no title chrome, so the header is a markup line — the same
        // approach the command palette takes for its footer hint.
        AddChild(Controls.Markup()
            .AddLine($"[{_mutedMarkup}]FILTER · {title}[/]")
            .WithAlignment(SharpConsoleUI.Layout.HorizontalAlignment.Center)
            .Build());

        _searchInput = Controls.Prompt("/ ")
            .WithAlignment(SharpConsoleUI.Layout.HorizontalAlignment.Stretch)
            .WithMargin(1, 0, 1, 0)
            .Build();
        _searchInput.Input = initialFilter;
        AddChild(_searchInput);

        AddChild(Controls.RuleBuilder()
            .WithColor(dimChrome)
            .Build());

        _resultCaption = Controls.Markup()
            .AddLine($"[{_mutedMarkup}]Type to filter · Enter apply · Esc cancel[/]")
            .WithAlignment(SharpConsoleUI.Layout.HorizontalAlignment.Center)
            .Build();
        AddChild(_resultCaption);

        var w = Math.Min(FilterMaxWidth, Math.Max(20, windowWidth - 4));
        var h = Math.Min(FilterHeight, Math.Max(3, windowHeight - 4));
        var x = (windowWidth - w) / 2;

        // Row 1 — anchored near the top (row 0 = menu bar), matching the command palette.
        PortalBounds = new SRectangle(x, 1, w, h);

        _searchInput.InputChanged += (_, text) => TextChanged?.Invoke(this, text ?? string.Empty);

        // Portals route focus through PortalFocusedControl rather than the window's FocusManager,
        // so this is what puts the caret in the search prompt.
        PortalFocusedControl = _searchInput;

        SetFocusOnFirstChild();
    }

    /// <summary>
    /// Raised on every keystroke with the current filter text, so the view re-filters live.
    /// </summary>
    public event EventHandler<string>? TextChanged;

    /// <summary>
    /// Raised when the user presses Enter to close the portal and keep the filter applied.
    /// </summary>
    public event EventHandler? Committed;

    /// <summary>
    /// Raised when the user presses Escape to close the portal and restore the previous filter.
    /// Distinct from the base <see cref="PortalContentBase.DismissRequested"/>, which the framework
    /// fires on outside-click. Subscribers should handle both to cover all dismissal paths.
    /// </summary>
    public event EventHandler? Cancelled;

    /// <summary>
    /// Updates the caption that reports how many rows the current filter matches.
    /// </summary>
    /// <param name="matches">The number of matching rows.</param>
    /// <param name="total">The total number of rows before filtering.</param>
    public void SetResultCount(int matches, int total)
    {
        var text = matches == total
            ? $"[{_mutedMarkup}]{total} rows · Enter apply · Esc cancel[/]"
            : $"[{_mutedMarkup}]{matches} of {total} rows · Enter apply · Esc cancel[/]";

        _resultCaption.SetContent([text]);
    }

    /// <summary>
    /// Forwards mouse events to the hosted prompt so clicking inside the portal positions the caret
    /// rather than falling through to the table beneath.
    /// </summary>
    /// <param name="args">The mouse event.</param>
    /// <returns><see langword="true"/> when the event was consumed.</returns>
    public override bool ProcessMouseEvent(MouseEventArgs args)
    {
        if (args.HasAnyFlag(SharpConsoleUI.Drivers.MouseFlags.ReportMousePosition))
        {
            if (_searchInput is IMouseAwareControl mouseAware && mouseAware.WantsMouseEvents)
            {
                mouseAware.ProcessMouseEvent(args);
            }

            return true;
        }

        return base.ProcessMouseEvent(args);
    }

    /// <summary>
    /// Processes a keystroke while the filter portal is open.
    /// </summary>
    /// <remarks>
    /// <para>Esc raises <see cref="Cancelled"/>; Enter raises <see cref="Committed"/>.</para>
    /// <para>All other keys are forwarded to the search prompt, which fires the live re-filter.</para>
    /// <para>Always returns <see langword="true"/> to swallow every key while the portal is visible.</para>
    /// </remarks>
    /// <param name="key">The key to process.</param>
    /// <returns><see langword="true"/> — the portal consumes all keys while open.</returns>
    public new bool ProcessKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                Cancelled?.Invoke(this, EventArgs.Empty);
                return true;

            case ConsoleKey.Enter:
                Committed?.Invoke(this, EventArgs.Empty);
                return true;
        }

        // Typing and backspace go to the focused child — the search prompt — which fires
        // InputChanged on every character and drives the live re-filter.
        base.ProcessKey(key);
        return true;
    }
}
