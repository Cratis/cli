// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI.Layout;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// Applications navigation item — filterable table of registered OAuth applications with a detail pane.
/// </summary>
public class ApplicationsView : FilterableTableView<ApplicationResponse>
{
    /// <inheritdoc/>
    protected override IReadOnlyList<(string Name, TextJustification Justify, int? Width)> Columns =>
    [
        ("ClientId", TextJustification.Left, null),
        ("Active", TextJustification.Left, 8),
        ("Created", TextJustification.Left, 30)
    ];

    /// <inheritdoc/>
    protected override string DetailPanelHeader => "APPLICATION";

    /// <inheritdoc/>
    protected override string? PageTitle => "APPLICATIONS";

    /// <inheritdoc/>
    protected override string EmptyStateMessage => "No applications registered.";

    /// <inheritdoc/>
    protected override IEnumerable<ApplicationResponse> GetItems(WorkbenchData data) =>
        data.Applications.OrderBy(a => a.ClientId);

    /// <inheritdoc/>
    protected override string GetKey(ApplicationResponse item) => item.Id.ToString();

    /// <inheritdoc/>
    protected override string[] BuildRow(ApplicationResponse item)
    {
        var activeColor = item.IsActive ? Theme.Success.ToMarkup() : Theme.Muted.ToMarkup();
        return
        [
            item.ClientId,
            $"[{activeColor}]{(item.IsActive ? "Yes" : "No")}[/]",
            item.CreatedAt.ToString()
        ];
    }

    /// <inheritdoc/>
    protected override string RenderDetail(ApplicationResponse? item, WorkbenchData? data)
    {
        if (item is null)
        {
            return SelectPrompt("an application");
        }

        var mut = Theme.Muted.ToMarkup();
        var suc = Theme.Success.ToMarkup();
        var activeColor = item.IsActive ? suc : mut;

        return string.Join(
            "\n",
            $"[{mut}]Id[/]        {item.Id}",
            $"[{mut}]ClientId[/]  {item.ClientId}",
            $"[{mut}]Active[/]    [{activeColor}]{(item.IsActive ? "Yes" : "No")}[/]",
            $"[{mut}]Created[/]   {item.CreatedAt}");
    }

    /// <inheritdoc/>
    protected override bool MatchesFilter(ApplicationResponse item, string filter) =>
        item.ClientId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        item.Id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase);
}
