// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Affected;

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Replays merged changes offline against the coverage maps committed before each, to measure the selection.
/// </summary>
[LlmDescription("Replays the last N merged changes of a branch offline, each against the coverage maps committed before it, and reports the median fraction of tests the selection would have run and, given the failures of the real runs, every failure it would have missed. Needs maps in the history, so it works after the maps have been produced on merge.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("replay", "Replay merged changes to measure the selection", Branch = typeof(AffectedBranch))]
[CliExample("affected", "replay", "--branch", "origin/main", "--count", "100")]
[CliExample("affected", "replay", "--failures", "failures.json")]
[LlmOutputAdvice("json", "Per change: whether a full run was forced, projects run, selected and total units, and any missed failures.")]
[LlmOption("--branch", "string", "Branch whose first-parent history is replayed (default HEAD)")]
[LlmOption("--count", "int", "How many changes to replay (default 100)")]
[LlmOption("--failures", "string", "JSON file mapping a commit to the tests that failed in its real run")]
[LlmOption("--report-directory", "string", "Where the report is written (default affected-report)")]
public sealed class AffectedReplayCommand : AsyncCommand<AffectedReplaySettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, AffectedReplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            IReadOnlyDictionary<string, IReadOnlyList<string>> failures = settings.Failures is null
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, IReadOnlyList<string>>>(await File.ReadAllTextAsync(settings.Failures, cancellationToken)) ?? [];
            var git = GitRepository.Open(Directory.GetCurrentDirectory());
            var report = new ReplayStudy(git).Run(settings.Branch, settings.Count, failures);

            Directory.CreateDirectory(settings.ReportDirectory);
            await File.WriteAllTextAsync(Path.Combine(settings.ReportDirectory, "replay.md"), report.ToMarkdown(), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(settings.ReportDirectory, "replay.json"), JsonSerializer.Serialize(report, OutputFormatter.IndentedJsonSerializerOptions), cancellationToken);

            OutputFormatter.Write(
                format,
                report.Commits,
                ["Commit", "Full run", "Projects run", "Selected units", "Misses"],
                commit => [commit.Commit[..Math.Min(10, commit.Commit.Length)], commit.RunEverything ? "yes" : "no", $"{commit.ProjectsRun} of {commit.TestProjects}", $"{commit.SelectedUnits} of {commit.TotalUnits}", commit.Misses.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)],
                commit => commit.Commit);
            OutputFormatter.WriteMessage(format, $"Median selected fraction {report.MedianSelectedFraction:P1}; changes with a missed failure: {report.MissedChanges}.");
            return report.MissedChanges == 0 ? ExitCodes.Success : ExitCodes.DriftDetected;
        }
        catch (Exception exception) when (exception is AffectedException or IOException or JsonException)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }
}
