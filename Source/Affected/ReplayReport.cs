// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Affected;

/// <summary>
/// The outcome of replaying merged changes against the coverage maps that existed before them.
/// </summary>
/// <param name="Commits">One entry per replayed change.</param>
public record ReplayReport(IReadOnlyList<ReplayCommit> Commits)
{
    /// <summary>
    /// Gets the number of changes whose real failures the selection would have missed.
    /// </summary>
    public int MissedChanges => Commits.Count(commit => commit.Misses.Count > 0);

    /// <summary>
    /// Gets the median selected fraction of units across the replayed changes.
    /// </summary>
    public double MedianSelectedFraction
    {
        get
        {
            var sorted = Commits.Select(commit => commit.SelectedFraction).Order().ToList();
            if (sorted.Count == 0)
            {
                return 1;
            }

            var middle = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
        }
    }

    /// <summary>
    /// Renders the report as Markdown.
    /// </summary>
    /// <returns>The Markdown text.</returns>
    public string ToMarkdown()
    {
        List<string> lines =
        [
            "# Replay study",
            string.Empty,
            string.Create(CultureInfo.InvariantCulture, $"- Changes replayed: {Commits.Count}"),
            string.Create(CultureInfo.InvariantCulture, $"- Changes with a missed failure: {MissedChanges}"),
            string.Create(CultureInfo.InvariantCulture, $"- Median selected fraction: {MedianSelectedFraction:P1}"),
            string.Create(CultureInfo.InvariantCulture, $"- Changes that forced a full run: {Commits.Count(commit => commit.RunEverything)}"),
            string.Empty,
            "| Commit | Full run | Projects run | Selected units | Misses |",
            "| --- | --- | --- | --- | --- |"
        ];
        lines.AddRange(Commits.Select(commit => string.Create(
            CultureInfo.InvariantCulture,
            $"| `{commit.Commit[..Math.Min(10, commit.Commit.Length)]}` | {(commit.RunEverything ? "yes" : "no")} | {commit.ProjectsRun} of {commit.TestProjects} | {commit.SelectedUnits} of {commit.TotalUnits} | {commit.Misses.Count} |")));
        return string.Join('\n', lines) + "\n";
    }
}
