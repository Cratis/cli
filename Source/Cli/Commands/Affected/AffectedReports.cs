// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected;

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Writes selection reports where later workflow steps and the people reading the run expect them.
/// </summary>
public static class AffectedReports
{
    /// <summary>
    /// Writes selection.json and selection.md, and appends the Markdown to the GitHub step summary when running in Actions.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="directory">The folder to write to.</param>
    public static void Write(SelectionReport report, string directory)
    {
        Directory.CreateDirectory(directory);
        var markdown = ReportWriter.ToMarkdown(report);
        File.WriteAllText(Path.Combine(directory, "selection.json"), ReportWriter.ToJson(report));
        File.WriteAllText(Path.Combine(directory, "selection.md"), markdown);

        var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrEmpty(summary))
        {
            File.AppendAllText(summary, markdown + Environment.NewLine);
        }
    }

    /// <summary>
    /// Writes the selection to the console in the requested format.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="report">The report.</param>
    public static void WriteConsole(string format, SelectionReport report)
    {
        OutputFormatter.Write(
            format,
            report.Projects,
            ["Test project", "Mode", "Selected", "Why"],
            plan =>
            [
                plan.Selection.Project,
                plan.Selection.Mode.ToString(),
                plan.Selection.TotalUnits > 0 ? $"{plan.Selection.SelectedUnits} of {plan.Selection.TotalUnits}" : "-",
                string.Join("; ", plan.Selection.Reasons.Take(2))
            ],
            plan => plan.Selection.Mode == Cratis.Affected.SelectionMode.Skip ? string.Empty : plan.Selection.Project);
    }
}
