// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpConsoleUI.Layout;
using SharpConsoleUI.Themes;

namespace Cratis.Cli.Commands.Chronicle.Workbench;

/// <summary>
/// Recommendations tab — filterable table of pending recommendations with apply/ignore actions.
/// </summary>
public class RecommendationsView : FilterableTableView<RecommendationDetailsResponse>
{
    /// <summary>Gets the currently selected recommendation, or <see langword="null"/> if none is selected.</summary>
    public RecommendationDetailsResponse? SelectedRecommendation => SelectedItem;

    /// <inheritdoc/>
    public override string ViewHelp =>
        "Lists pending recommendations suggested by Chronicle.\n" +
        "  [A]  Apply the selected recommendation\n" +
        "  [I]  Ignore the selected recommendation\n" +
        "  [Space]  Check / uncheck row for bulk operations\n" +
        "  Check rows + toolbar / right-click → bulk apply / ignore";

    /// <summary>
    /// Gets or sets the callback invoked when the user applies a recommendation.
    /// </summary>
    public Action<RecommendationDetailsResponse>? OnApply { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user ignores a recommendation.
    /// </summary>
    public Action<RecommendationDetailsResponse>? OnIgnore { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user requests a bulk apply of all checked recommendations.
    /// </summary>
    public Action<IReadOnlyList<RecommendationDetailsResponse>>? OnApplyAll { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user requests a bulk ignore of all checked recommendations.
    /// </summary>
    public Action<IReadOnlyList<RecommendationDetailsResponse>>? OnIgnoreAll { get; set; }

    /// <summary>
    /// Gets all recommendations that are currently checked (checkbox mode).
    /// </summary>
    public IReadOnlyList<RecommendationDetailsResponse> Checked => CheckedItems;

    /// <inheritdoc/>
    protected override IReadOnlyList<(string Name, TextJustification Justify, int? Width)> Columns =>
    [
        ("Name", TextJustification.Left, null),
        ("Type", TextJustification.Left, 20)
    ];

    /// <inheritdoc/>
    protected override string DetailPanelHeader => "RECOMMENDATION";

    /// <inheritdoc/>
    protected override ColorRole DetailColorRole => ColorRole.Warning;

    /// <inheritdoc/>
    protected override bool HasCheckboxMode => true;

    /// <inheritdoc/>
    protected override string? PageTitle => "RECOMMENDATIONS";

    /// <inheritdoc/>
    protected override string EmptyStateMessage => "No pending recommendations.";

    /// <inheritdoc/>
    protected override IReadOnlyList<ViewAction> GetToolbarActionTemplate()
    {
        List<ViewAction> actions = [];
        if (OnApply is not null)
        {
            actions.Add(SingleAction("Apply recommendation", ConsoleKey.A, item => OnApply(item), isDestructive: true));
        }

        if (OnIgnore is not null)
        {
            actions.Add(SingleAction("Ignore recommendation", ConsoleKey.I, item => OnIgnore(item), isDestructive: true));
        }

        if (OnApplyAll is not null)
        {
            actions.Add(BulkAction("Apply", items => OnApplyAll(items), isDestructive: true));
        }

        if (OnIgnoreAll is not null)
        {
            actions.Add(BulkAction("Ignore", items => OnIgnoreAll(items), isDestructive: true));
        }

        return actions;
    }

    /// <inheritdoc/>
    protected override IEnumerable<RecommendationDetailsResponse> GetItems(WorkbenchData data) => data.Recommendations;

    /// <inheritdoc/>
    protected override string GetKey(RecommendationDetailsResponse item) => item.Id.ToString();

    /// <inheritdoc/>
    protected override string[] BuildRow(RecommendationDetailsResponse item) =>
        [item.Name ?? item.Id.ToString(), item.Type ?? "—"];

    /// <inheritdoc/>
    protected override string RenderDetail(RecommendationDetailsResponse? item, WorkbenchData? data)
    {
        if (item is null)
        {
            return SelectPrompt("a recommendation");
        }

        var mut = Theme.Muted.ToMarkup();

        var lines = new List<string>
        {
            $"[{mut}]Name[/]  {item.Name ?? item.Id.ToString()}",
            $"[{mut}]Type[/]  {item.Type ?? "—"}"
        };

        if (!string.IsNullOrEmpty(item.Description))
        {
            lines.Add(string.Empty);
            lines.Add($"[{mut}]Description:[/]");
            lines.Add($"  {item.Description}");
        }

        return string.Join('\n', lines);
    }

    /// <inheritdoc/>
    protected override bool MatchesFilter(RecommendationDetailsResponse item, string filter) =>
        (item.Name ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        (item.Type ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        item.Id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase);
}
