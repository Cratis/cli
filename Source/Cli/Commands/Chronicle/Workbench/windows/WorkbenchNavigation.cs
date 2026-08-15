// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI;
using SharpConsoleUI.Builders;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Themes;
using UITableRow = SharpConsoleUI.Controls.TableRow;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// Builds and manages the navigation side pane, badge counts, and the event store / namespace picker overlays.
/// All navigation items are driven by <see cref="WorkbenchViewRegistry"/> — add a view there and it appears here automatically.
/// </summary>
/// <param name="windowSystem">The SharpConsoleUI window system.</param>
/// <param name="theme">The workbench theme used to resolve chrome and section accent colors.</param>
/// <param name="views">The ordered array of <see cref="IWorkbenchView"/> instances — must match <see cref="WorkbenchViewRegistry.All"/> order.</param>
/// <param name="settings">Workbench settings — used to resolve the active event store and namespace.</param>
/// <param name="getActiveEventStore">Returns the currently active event store name, or <see langword="null"/> for the default.</param>
/// <param name="getActiveNamespace">Returns the currently active namespace name, or <see langword="null"/> for the default.</param>
/// <param name="onStoreSwitch">Invoked with the newly selected event store name when the user picks one from the picker overlay.</param>
/// <param name="onNamespaceSwitch">Invoked with the newly selected namespace name when the user picks one from the picker overlay.</param>
/// <param name="onDataNeeded">Invoked when navigation changes require a data refresh.</param>
/// <param name="getLatestData">Returns the latest cached <see cref="WorkbenchData"/> snapshot, or <see langword="null"/> if none available yet.</param>
public class WorkbenchNavigation(
    ConsoleWindowSystem windowSystem,
    WorkbenchTheme theme,
    IWorkbenchView[] views,
    WorkbenchSettings settings,
    Func<string?> getActiveEventStore,
    Func<string?> getActiveNamespace,
    Action<string> onStoreSwitch,
    Action<string> onNamespaceSwitch,
    Action onDataNeeded,
    Func<WorkbenchData?> getLatestData)
{
    // ── View index constants ───────────────────────────────────────────────────
    // Derived from WorkbenchViewRegistry — the registry position IS the index.
    // Never hardcode these values; never pass them to NavigationView.SelectedIndex directly.
    // Always navigate via NavigateTo(IndexXxx) which converts to the header-inclusive index.

    /// <summary>View index for Overview.</summary>
    public static readonly int IndexOverview = WorkbenchViewRegistry.IndexOf<OverviewView>();

    /// <summary>View index for Observers.</summary>
    public static readonly int IndexObservers = WorkbenchViewRegistry.IndexOf<ObserversView>();

    /// <summary>View index for Failures.</summary>
    public static readonly int IndexFailures = WorkbenchViewRegistry.IndexOf<FailedPartitionsView>();

    /// <summary>View index for Jobs.</summary>
    public static readonly int IndexJobs = WorkbenchViewRegistry.IndexOf<JobsView>();

    /// <summary>View index for Recommendations.</summary>
    public static readonly int IndexRecommendations = WorkbenchViewRegistry.IndexOf<RecommendationsView>();

    /// <summary>View index for Event Sequences.</summary>
    public static readonly int IndexEventSequences = WorkbenchViewRegistry.IndexOf<EventSequencesView>();

    /// <summary>View index for Event Types.</summary>
    public static readonly int IndexEventTypes = WorkbenchViewRegistry.IndexOf<EventTypesView>();

    /// <summary>View index for Projections.</summary>
    public static readonly int IndexProjections = WorkbenchViewRegistry.IndexOf<ProjectionsView>();

    /// <summary>View index for Read Models.</summary>
    public static readonly int IndexReadModels = WorkbenchViewRegistry.IndexOf<ReadModelsView>();

    /// <summary>View index for Event Stores.</summary>
    public static readonly int IndexEventStores = WorkbenchViewRegistry.IndexOf<EventStoresView>();

    /// <summary>View index for Namespaces.</summary>
    public static readonly int IndexNamespaces = WorkbenchViewRegistry.IndexOf<NamespacesView>();

    const int PickerOverlayWidth = 54;
    const int MaxPickerOverlayHeight = 24;

    /// <summary>
    /// Floor for the picker height. Sizing on row count alone made a two-item picker mostly chrome.
    /// </summary>
    const int MinPickerOverlayHeight = 14;
    const int PickerOverlayHeightPadding = 6;
    const int NavExpandedThreshold = 90;
    const int NavCompactThreshold = 40;

    /// <summary>Identity of each status-bar chooser, so re-opening the same one toggles it closed.</summary>
    static readonly object _eventStorePortalKey = new();

    /// <summary>Identity of the namespace chooser.</summary>
    static readonly object _namespacePortalKey = new();

    /// <summary>Identity of the theme chooser.</summary>
    static readonly object _themePortalKey = new();

    /// <summary>Identity of the refresh-interval chooser.</summary>
    static readonly object _intervalPortalKey = new();

    /// <summary>Refresh intervals offered by the interval chooser, in seconds.</summary>
    static readonly int[] _refreshIntervals = [1, 2, 5, 10, 30, 60];

    /// <summary>Header items paired with their section accent, used to re-apply colors on theme change.</summary>
    readonly List<(NavigationItem Header, WorkbenchSectionAccent Accent)> _sectionHeaders = [];

    NavigationItem? _observersItem;
    NavigationItem? _failuresItem;
    NavigationItem? _recommendationsItem;
    NavigationView? _navView;
    int _currentViewIndex;

    /// <summary>Gets the built <see cref="NavigationView"/> control. Only available after <see cref="BuildNavigationView"/> has been called.</summary>
    public NavigationView? NavView => _navView;

    /// <summary>
    /// Gets the zero-based item-only index of the currently active view.
    /// Excludes header entries, aligns with <c>IndexXxx</c> constants, and can be used directly to index into <c>views[]</c>.
    /// </summary>
    public int CurrentViewIndex => _currentViewIndex;

    /// <summary>Gets the Observers navigation item (used to set badge counts). Only available after <see cref="BuildNavigationView"/>.</summary>
    public NavigationItem? ObserversItem => _observersItem;

    /// <summary>Gets the Failures navigation item (used to set badge counts). Only available after <see cref="BuildNavigationView"/>.</summary>
    public NavigationItem? FailuresItem => _failuresItem;

    /// <summary>Gets the Recommendations navigation item (used to set badge counts). Only available after <see cref="BuildNavigationView"/>.</summary>
    public NavigationItem? RecommendationsItem => _recommendationsItem;

    /// <summary>
    /// Builds the navigation view from <see cref="WorkbenchViewRegistry"/> — headers and items are derived automatically.
    /// Wires the selection-changed callback and captures the badge item references.
    /// </summary>
    /// <returns>The fully configured <see cref="NavigationView"/>.</returns>
    public NavigationView BuildNavigationView()
    {
        // Declare navView before the builder chain so the lambda can capture it.
        // It will be null when OnSelectedItemChanged fires during the initial build-time
        // auto-selection; the guard below handles that case gracefully.
        NavigationView? navView = null;

        navView = Controls.NavigationView()
            .WithNavWidth(28)
            .WithPaneHeader($"[bold {theme.Accent.ToMarkup()}] ◆ CHRONICLE[/]")
            .WithContentBorder(BorderStyle.Rounded)
            .WithContentPadding(1, 0, 1, 0)
            .WithPaneDisplayMode(NavigationViewDisplayMode.Auto)
            .WithExpandedThreshold(NavExpandedThreshold)
            .WithCompactThreshold(NavCompactThreshold)
            .WithName("MainNav")
            .Fill()
            .OnSelectedItemChanged((_, e) =>
            {
                // navView may be null while Build() is still executing (first auto-selection).
                // In that case _currentViewIndex stays at its default of 0 (Overview), which is correct.
                if (navView is null)
                {
                    return;
                }

                var oldViewIdx = ToViewIndex(navView, e.OldIndex);
                if (oldViewIdx >= 0 && oldViewIdx < views.Length)
                {
                    views[oldViewIdx].IsActive = false;
                }

                var idx = ToViewIndex(navView, e.NewIndex);
                _currentViewIndex = idx >= 0 ? idx : 0;

                if (idx < 0 || idx >= views.Length)
                {
                    return;
                }

                var snapshot = getLatestData();
                if (snapshot is not null)
                {
                    views[idx].UpdateData(snapshot);
                }
                else
                {
                    onDataNeeded();
                }

                views[idx].IsActive = true;
            })
            .Build();

        // Add headers and items driven entirely by the registry.
        // Adding a new view to WorkbenchViewRegistry.All automatically adds it here.
        WorkbenchSection? lastSection = null;
        NavigationItem? currentHeader = null;

        for (var i = 0; i < WorkbenchViewRegistry.All.Count; i++)
        {
            var def = WorkbenchViewRegistry.All[i];
            var viewIndex = i;

            if (!ReferenceEquals(def.Section, lastSection))
            {
                currentHeader = navView!.AddHeader(def.Section.Title, theme.SectionAccent(def.Section.Accent));
                _sectionHeaders.Add((currentHeader, def.Section.Accent));
                lastSection = def.Section;
            }

            var navItem = navView!.AddItemToHeader(currentHeader!, def.NavText, def.NavIcon, def.NavSubtitle);
            navView.SetItemContent(navItem, panel => views[viewIndex].PopulateContent(panel, windowSystem));
        }

        var allItems = navView!.Items;
        _observersItem = FindItemByText(allItems, WorkbenchViewRegistry.All[IndexObservers].NavText);
        _failuresItem = FindItemByText(allItems, WorkbenchViewRegistry.All[IndexFailures].NavText);
        _recommendationsItem = FindItemByText(allItems, WorkbenchViewRegistry.All[IndexRecommendations].NavText);

        _navView = navView;

        theme.Changed += ApplyThemeColors;

        // Guard: registry count must equal views.Length — they're both built from the same registry.
        var nonHeaderCount = allItems.Count(i => i.ItemType != NavigationItemType.Header);
        System.Diagnostics.Debug.Assert(
            nonHeaderCount == views.Length,
            $"Nav has {nonHeaderCount} selectable items but _views has {views.Length}. Both must match {nameof(WorkbenchViewRegistry)}.");

        return navView;
    }

    /// <summary>Navigates to the specified view by index. No-op when the index is out of range.</summary>
    /// <param name="viewIndex">Zero-based item-only view index (use <c>IndexXxx</c> constants).</param>
    public void NavigateTo(int viewIndex)
    {
        if (_navView is null || viewIndex < 0 || viewIndex >= views.Length)
        {
            return;
        }

        var navIndex = ToNavIndex(_navView, viewIndex);
        if (navIndex >= 0)
        {
            _navView.SelectedIndex = navIndex;
        }
    }

    /// <summary>Updates the badge subtitles on the Observers, Failures, and Recommendations nav items.</summary>
    /// <param name="data">The latest workbench data snapshot.</param>
    public void UpdateNavBadges(WorkbenchData data)
    {
        var problemCount = data.DisconnectedObservers + data.ReplayingObservers;

        if (_observersItem is NavigationItem observersItem)
        {
            observersItem.Subtitle = problemCount > 0 ? $"⚠{problemCount}" : string.Empty;
        }

        if (_failuresItem is NavigationItem failuresItem)
        {
            failuresItem.Subtitle = data.FailedPartitions.Count > 0
                ? data.FailedPartitions.Count.ToString()
                : string.Empty;
        }

        if (_recommendationsItem is NavigationItem recommendationsItem)
        {
            recommendationsItem.Subtitle = data.Recommendations.Count > 0
                ? data.Recommendations.Count.ToString()
                : string.Empty;
        }

        // No explicit Invalidate: NavigationItem.Subtitle is reactive — its setter self-invalidates
        // only when a value actually changes. Forcing an unconditional relayout here every tick is
        // redundant and (because a window relayout currently drops open portals) would close the
        // command palette on each refresh while no badge has changed.
    }

    /// <summary>Opens a modal picker that lets the user select a different event store.</summary>
    public void OpenEventStorePicker()
    {
        var snapshot = getLatestData();
        if (snapshot is null)
        {
            return;
        }

        ShowStringPickerOverlay(
            " Switch Event Store ",
            "Event Store",
            "EventStorePickerTable",
            [.. snapshot.EventStoreNames.Order()],
            getActiveEventStore() ?? settings.ResolveEventStore(),
            onStoreSwitch);
    }

    /// <summary>Opens a modal picker that lets the user select a different namespace.</summary>
    public void OpenNamespacePicker()
    {
        var snapshot = getLatestData();
        if (snapshot is null)
        {
            return;
        }

        ShowStringPickerOverlay(
            " Switch Namespace ",
            "Namespace",
            "NamespacePickerTable",
            [.. snapshot.NamespaceNames.Order()],
            getActiveNamespace() ?? settings.ResolveNamespace(),
            onNamespaceSwitch);
    }

    /// <summary>
    /// Opens the event-store chooser as a portal above the status bar — the click counterpart to the
    /// Ctrl+E dialog, for picking without leaving the current view.
    /// </summary>
    /// <param name="anchorRight">Right edge of the status-bar segment that opened it, for alignment.</param>
    public void OpenEventStorePortal(int? anchorRight = null)
    {
        var snapshot = getLatestData();
        if (snapshot is null)
        {
            return;
        }

        var active = getActiveEventStore() ?? settings.ResolveEventStore();
        WorkbenchStatusPortal.Open(
            windowSystem,
            theme,
            _eventStorePortalKey,
            "Event store",
            [.. snapshot.EventStoreNames.Order().Select(n => (Label: Marked(n, active), Value: n))],
            active,
            onStoreSwitch,
            anchorRight);
    }

    /// <summary>
    /// Opens the namespace chooser as a portal above the status bar — the click counterpart to the
    /// Ctrl+N dialog.
    /// </summary>
    /// <param name="anchorRight">Right edge of the status-bar segment that opened it, for alignment.</param>
    public void OpenNamespacePortal(int? anchorRight = null)
    {
        var snapshot = getLatestData();
        if (snapshot is null)
        {
            return;
        }

        var active = getActiveNamespace() ?? settings.ResolveNamespace();
        WorkbenchStatusPortal.Open(
            windowSystem,
            theme,
            _namespacePortalKey,
            "Namespace",
            [.. snapshot.NamespaceNames.Order().Select(n => (Label: Marked(n, active), Value: n))],
            active,
            onNamespaceSwitch,
            anchorRight);
    }

    /// <summary>
    /// Opens the theme chooser as a portal above the status bar, listing every registered theme.
    /// </summary>
    /// <param name="anchorLeft">Left edge of the Theme hint, so the chooser opens above it.</param>
    public void OpenThemePortal(int? anchorLeft = null)
    {
        // Every registered theme, not the three the old F9/F10/F11 slots exposed — the registry is
        // the full catalog and the portal has room to list it.
        var names = windowSystem.ThemeRegistryService.GetAvailableThemeNames();
        var active = windowSystem.ThemeStateService.CurrentTheme?.Name;

        // Two swatches per row from the theme's own primary and secondary colors, so the list shows
        // what each theme looks like rather than only what it is called.
        var entries = names.Order().Select(name =>
        {
            var candidate = windowSystem.ThemeRegistryService.GetTheme(name);
            var primary = (candidate?.PrimaryColor ?? theme.Muted).ToMarkup();
            var secondary = (candidate?.SecondaryColor ?? theme.Muted).ToMarkup();

            return (Label: $"[{primary}]██[/][{secondary}]██[/] {Marked(name, active)}", Value: name);
        });

        WorkbenchStatusPortal.Open(
            windowSystem,
            theme,
            _themePortalKey,
            "Theme",
            [.. entries],
            active,
            name => windowSystem.ThemeStateService.SwitchTheme(name),
            anchorLeft,
            alignLeft: true);
    }

    /// <summary>
    /// Opens the refresh-interval chooser as a portal above the status bar.
    /// </summary>
    /// <param name="onIntervalChosen">Invoked with the chosen interval in seconds.</param>
    /// <param name="anchorRight">Right edge of the status-bar segment that opened it, for alignment.</param>
    public void OpenIntervalPortal(Action<int> onIntervalChosen, int? anchorRight = null)
    {
        var active = settings.Interval.ToString(System.Globalization.CultureInfo.InvariantCulture);

        WorkbenchStatusPortal.Open(
            windowSystem,
            theme,
            _intervalPortalKey,
            "Refresh every",
            [.. _refreshIntervals.Select(seconds =>
            {
                var value = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return (Label: Marked($"{value}s", value == active ? value : null, value), Value: value);
            })],
            active,
            value => onIntervalChosen(int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
            anchorRight);
    }

    /// <summary>
    /// Prefixes the active entry with a marker so the current selection is obvious in the list.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="active">The active value to compare against.</param>
    /// <param name="compare">Value to compare instead of <paramref name="name"/>, when the label differs.</param>
    /// <returns>The display label.</returns>
    static string Marked(string name, string? active, string? compare = null) =>
        string.Equals(compare ?? name, active, StringComparison.Ordinal) ? $"► {name}" : $"  {name}";

    /// <summary>
    /// Converts a header-inclusive NavigationView item index to a zero-based view-only index.
    /// Takes the nav view directly so it works inside lambdas before <c>_navView</c> is assigned.
    /// Returns -1 for header entries or out-of-range indices.
    /// </summary>
    /// <param name="nav">The NavigationView to query.</param>
    /// <param name="navIndex">The header-inclusive index from the NavigationView.</param>
    /// <returns>The zero-based view-only index, or -1 if not applicable.</returns>
    static int ToViewIndex(NavigationView nav, int navIndex)
    {
        if (navIndex < 0)
        {
            return -1;
        }

        var items = nav.Items;
        if (navIndex >= items.Count || items[navIndex].ItemType == NavigationItemType.Header)
        {
            return -1;
        }

        var viewIdx = 0;
        for (var i = 0; i < navIndex; i++)
        {
            if (items[i].ItemType != NavigationItemType.Header)
            {
                viewIdx++;
            }
        }

        return viewIdx;
    }

    /// <summary>
    /// Converts a zero-based view-only index to the header-inclusive NavigationView item index
    /// required by <see cref="NavigationView.SelectedIndex"/>.
    /// Returns -1 when the view index is not found.
    /// </summary>
    /// <param name="nav">The NavigationView to query.</param>
    /// <param name="viewIndex">The zero-based view-only index.</param>
    /// <returns>The header-inclusive NavigationView index, or -1 if not found.</returns>
    static int ToNavIndex(NavigationView nav, int viewIndex)
    {
        if (viewIndex < 0)
        {
            return -1;
        }

        var items = nav.Items;
        var itemCount = 0;
        for (var navIdx = 0; navIdx < items.Count; navIdx++)
        {
            if (items[navIdx].ItemType != NavigationItemType.Header)
            {
                if (itemCount == viewIndex)
                {
                    return navIdx;
                }

                itemCount++;
            }
        }

        return -1;
    }

    static NavigationItem? FindItemByText(IReadOnlyList<NavigationItem> items, string text)
    {
        foreach (var item in items)
        {
            if (item.Text == text)
            {
                return item;
            }
        }

        return null;
    }

    void ApplyThemeColors()
    {
        foreach (var (header, accent) in _sectionHeaders)
        {
            // NavigationItem.HeaderColor is reactive — its setter self-invalidates when the value
            // changes, so no explicit Invalidate is needed (an unconditional one would be anti-reactive).
            header.HeaderColor = theme.SectionAccent(accent);
        }
    }

    void ShowStringPickerOverlay(
        string title,
        string columnHeader,
        string tableName,
        List<string> items,
        string activeItem,
        Action<string> onSelected)
    {
        var acc = theme.Accent.ToMarkup();

        var pickerTable = Controls.Table()
            .AddColumn(columnHeader, SharpConsoleUI.Layout.TextJustification.Left, null)
            .Interactive()
            .WithVerticalScrollbar(ScrollbarVisibility.Auto)
            .NoBorder()
            .StretchHorizontal()
            .ScrollbarGutter()
            .WithName(tableName)
            .Build();

        foreach (var name in items)
        {
            var label = name == activeItem ? $"[{acc}]► {name}[/]" : name;
            pickerTable.AddRow(new UITableRow([label]) { Tag = name });
        }

        pickerTable.VerticalAlignment = SharpConsoleUI.Layout.VerticalAlignment.Fill;

        // Sizing on the row count alone left a two-store picker as an eight-row box that was almost
        // entirely chrome, with the table squeezed into a couple of rows. A floor keeps a short list
        // in a dialog worth looking at, and the terminal height caps it so a long one still fits.
        var height = Math.Clamp(
            items.Count + PickerOverlayHeightPadding,
            MinPickerOverlayHeight,
            Math.Min(MaxPickerOverlayHeight, Math.Max(MinPickerOverlayHeight, Console.WindowHeight - 6)));

        Window? picker = null;
        void SelectCurrent()
        {
            if (pickerTable.SelectedRow?.Tag is string selected)
            {
                if (picker is not null)
                {
                    windowSystem.CloseWindow(picker, activateParent: true, force: false);
                }

                onSelected(selected);
            }
        }

        picker = WorkbenchUi.BuildDialog(
            windowSystem,
            theme,
            title,
            [pickerTable],
            [new DialogButton("Select", ColorRole.Primary, SelectCurrent)],
            new DialogOptions
            {
                FillBody = true,
                CloseOnAction = false,
                Width = PickerOverlayWidth,
                Height = height,
                OnKey = (key, _) =>
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        SelectCurrent();
                        return true;
                    }

                    return false;
                }
            });

        pickerTable.RowActivated += (_, _) => SelectCurrent();

        windowSystem.AddWindow(picker, activateWindow: true);
    }
}
