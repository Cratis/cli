// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Drawing;
using SharpConsoleUI.Events;
using SharpConsoleUI.Helpers;
using SharpConsoleUI.Layout;
using SPoint = System.Drawing.Point;
using SRectangle = System.Drawing.Rectangle;
using SSize = System.Drawing.Size;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// A floating portal listing the actions available for the row under the cursor, anchored at the
/// click position. Destructive actions are separated from the read-only ones, so a mutating action
/// is never where a reflexive click lands.
/// </summary>
/// <remarks>
/// <para>
/// The menu intercepts all keystrokes while open; the hosting window must forward
/// <c>PreviewKeyPressed</c> events to <see cref="ProcessKey"/> and set
/// <see cref="KeyPressedEventArgs.Handled"/> to <see langword="true"/> so arrow keys move the menu
/// selection rather than the table beneath it.
/// </para>
/// <para>
/// Choosing an item raises <see cref="ActionChosen"/>; Esc raises <see cref="EscapeRequested"/>.
/// Framework dismissals (outside-click) fire the base
/// <see cref="PortalContentBase.DismissRequested"/> event.
/// </para>
/// </remarks>
public sealed class WorkbenchContextMenuPortal : PortalContentContainer
{
    const int MenuMinWidth = 18;
    const int MenuMaxWidth = 46;

    readonly MenuControl _menu;
    readonly Dictionary<MenuItem, ViewAction> _itemMap = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchContextMenuPortal"/> class.
    /// </summary>
    /// <param name="actions">The actions to list. Callers should pass only enabled actions.</param>
    /// <param name="theme">The active workbench theme — used for menu colors so it matches the workbench chrome.</param>
    /// <param name="x">The click position in screen columns.</param>
    /// <param name="y">The click position in screen rows.</param>
    /// <param name="window">The host window, used to convert screen coordinates into buffer space.</param>
    public WorkbenchContextMenuPortal(
        IReadOnlyList<ViewAction> actions,
        WorkbenchTheme theme,
        int x,
        int y,
        Window window)
    {
        _menu = new MenuControl
        {
            Orientation = MenuOrientation.Vertical,
            DropdownBackgroundColor = theme.Surface,
            DropdownForegroundColor = theme.Foreground,
            DropdownHighlightBackgroundColor = theme.SelectedBg,
            DropdownHighlightForegroundColor = theme.Foreground,
            MenuBarBackgroundColor = theme.Surface,
            MenuBarForegroundColor = theme.Foreground,
            MenuBarHighlightBackgroundColor = theme.SelectedBg,
            MenuBarHighlightForegroundColor = theme.Foreground
        };

        BackgroundColor = theme.Surface;
        ForegroundColor = theme.Foreground;
        DismissOnOutsideClick = true;
        BorderStyle = BoxChars.Rounded;
        BorderColor = theme.Accent.Mix(theme.Background, 0.8);
        BorderBackgroundColor = theme.Surface;

        // Read-only actions first, a separator, then the destructive ones. The separation is the
        // point: a reflexive click at the top of the menu can never land on a mutating action.
        var safe = actions.Where(a => !a.IsDestructive).ToList();
        var destructive = actions.Where(a => a.IsDestructive).ToList();

        foreach (var action in safe)
        {
            AddAction(action);
        }

        if (safe.Count > 0 && destructive.Count > 0)
        {
            _menu.AddItem(new MenuItem { IsSeparator = true, IsEnabled = false });
        }

        foreach (var action in destructive)
        {
            AddAction(action);
        }

        // Portals route focus through PortalFocusedControl rather than the window's FocusManager,
        // so the menu only receives keys and renders its selection once this is set.
        PortalFocusedControl = _menu;

        _menu.ItemSelected += (_, item) =>
        {
            if (_itemMap.TryGetValue(item, out var action))
            {
                ActionChosen?.Invoke(this, action);
            }
        };

        AddChild(_menu);
        SetFocusOnFirstChild();

        var widest = actions.Max(a => a.Label.Length);
        var width = Math.Clamp(widest + 4, MenuMinWidth, MenuMaxWidth);
        var height = actions.Count + (safe.Count > 0 && destructive.Count > 0 ? 1 : 0) + 2;

        // Screen coordinates come from the mouse event; the portal is placed in window-buffer space,
        // so subtract the window origin and its border. A further row up puts the menu's first item
        // level with the row that was clicked rather than below it.
        var position = PortalPositioner.CalculateFromPoint(
            new SPoint(x - window.Left - 1, y - window.Top - 2),
            new SSize(width, height),
            new SRectangle(0, 0, window.Width - 2, window.Height - 2),
            PortalPlacement.BelowOrAbove,
            new SSize(MenuMinWidth, 3));

        PortalBounds = position.Bounds;
    }

    /// <summary>
    /// Raised when the user activates an item, carrying the chosen <see cref="ViewAction"/>.
    /// </summary>
    public event EventHandler<ViewAction>? ActionChosen;

    /// <summary>
    /// Raised when the user presses Escape to close the menu without choosing an action.
    /// Distinct from the base <see cref="PortalContentBase.DismissRequested"/>, which the framework
    /// fires on outside-click. Subscribers should handle both to cover all dismissal paths.
    /// </summary>
    public event EventHandler? EscapeRequested;

    /// <summary>
    /// Forwards mouse events to the hosted menu so hovering highlights an item and clicking selects
    /// it. Without this the menu never sees the mouse and can only be driven by keyboard.
    /// </summary>
    /// <param name="args">The mouse event.</param>
    /// <returns><see langword="true"/> when the event was consumed.</returns>
    public override bool ProcessMouseEvent(MouseEventArgs args)
    {
        if (args.HasAnyFlag(SharpConsoleUI.Drivers.MouseFlags.ReportMousePosition))
        {
            if (_menu is IMouseAwareControl mouseAware && mouseAware.WantsMouseEvents)
            {
                mouseAware.ProcessMouseEvent(args);
            }

            return true;
        }

        return base.ProcessMouseEvent(args);
    }

    /// <summary>
    /// Processes a keystroke while the menu is open. Esc closes it, a shortcut key runs its action
    /// directly, and everything else is handled by the menu itself (arrows, Enter).
    /// </summary>
    /// <param name="key">The key to process.</param>
    /// <returns><see langword="true"/> — the menu consumes all keys while open.</returns>
    public new bool ProcessKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            EscapeRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // A shortcut key typed while the menu is open runs that action directly.
        var match = _itemMap.Values.FirstOrDefault(a => a.TriggerKey == key.Key && a.TriggerModifiers == key.Modifiers);
        if (match is not null)
        {
            ActionChosen?.Invoke(this, match);
            return true;
        }

        base.ProcessKey(key);
        return true;
    }

    void AddAction(ViewAction action)
    {
        // No Shortcut: the key hint is already on the toolbar button, and repeating it here crowds a
        // menu that is being driven by the mouse. The key still works while the menu is open.
        var item = new MenuItem
        {
            Text = action.Label,
            IsEnabled = true
        };

        _menu.AddItem(item);
        _itemMap[item] = action;
    }
}
