// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Affected;

/// <summary>
/// Renders a selection report as JSON for later steps and Markdown for people.
/// </summary>
public static class ReportWriter
{
    const int ReasonsShown = 5;

    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Renders the report as JSON.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns>The JSON text.</returns>
    public static string ToJson(SelectionReport report) => JsonSerializer.Serialize(report, _json);

    /// <summary>
    /// Renders the report as Markdown, suitable for a step summary.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns>The Markdown text.</returns>
    public static string ToMarkdown(SelectionReport report)
    {
        List<string> lines =
        [
            "## Affected tests",
            string.Empty,
            $"Mode `{report.Mode}`, level `{report.Level}`, `{Short(report.From)}`..`{Short(report.To)}`.",
            string.Empty
        ];

        if (report.RunEverything)
        {
            lines.AddRange([$"**Everything runs.** {string.Join("; ", report.Reasons.Take(ReasonsShown))}", string.Empty]);
        }

        lines.AddRange(["| Test project | Mode | Selected | Why |", "| --- | --- | --- | --- |"]);
        lines.AddRange(report.Projects.Select(plan =>
        {
            var selection = plan.Selection;
            var counts = selection.TotalUnits > 0 ? $"{selection.SelectedUnits} of {selection.TotalUnits}" : "-";
            var why = string.Join("; ", selection.Reasons.Take(ReasonsShown)).Replace("|", "\\|", StringComparison.Ordinal);
            return $"| `{selection.Project}` | {selection.Mode} | {counts} | {why} |";
        }));
        return string.Join('\n', lines) + "\n";
    }

    static string Short(string revision) => revision.Length > 10 ? revision[..10] : revision;
}
