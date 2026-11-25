// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected;

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Selects tests for a change and runs them. On the default branch it runs everything with coverage attribution and refreshes the coverage maps.
/// </summary>
[LlmDescription("Selects the tests a change can reach and runs them, writing TRX results. With --mode merge it runs every test unit in isolation under dotnet-coverage, writes one deterministic coverage map per test project to .cratis/affected/maps, and deletes maps of removed test projects; it never commits. Exit code is the test result.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("run", "Select and run the tests a change can reach", Branch = typeof(AffectedBranch))]
[CliExample("affected", "run", "--from", "origin/main")]
[CliExample("affected", "run", "--mode", "merge")]
[LlmOutputAdvice("plain", "The test runner's own output, followed by the selection table.")]
[LlmOption("--from", "string", "Ref the change is measured against (default origin/main)")]
[LlmOption("--to", "string", "Ref being tested (default HEAD)")]
[LlmOption("--mode", "string", "pr, merge or nightly (default pr)")]
[LlmOption("--level", "string", "project or test (default test)")]
[LlmOption("--run-all", "bool", "Force a full run")]
[LlmOption("--results-directory", "string", "Where TRX results are written (default affected-results)")]
[LlmOption("--collector", "string", "Coverage collector for merge mode (default dotnet-coverage)")]
public sealed class AffectedRunCommand : AsyncCommand<AffectedRunSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, AffectedRunSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var options = settings.ToPlanOptions();
            var git = GitRepository.Open(Directory.GetCurrentDirectory());
            var runner = new XunitV3Runner(git.Root, settings.Configuration, settings.Framework);
            var report = await new AffectedPlanner(git, runner).PlanAsync(options, cancellationToken);
            AffectedReports.Write(report, settings.ReportDirectory);
            AffectedReports.WriteConsole(format, report);

            if (options.Mode == PlanMode.Merge && !options.ForceAll)
            {
                return await UpdateMapsAsync(git, runner, settings, format, cancellationToken);
            }

            return await new AffectedRunner(runner).RunAsync(report, settings.ResultsDirectory, cancellationToken);
        }
        catch (Exception exception) when (exception is AffectedException or ArgumentException)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }

    static async Task<int> UpdateMapsAsync(GitRepository git, XunitV3Runner runner, AffectedRunSettings settings, string format, CancellationToken cancellationToken)
    {
        var updater = new MapUpdater(git, runner, new CoverageAttributor(runner, git, settings.Collector));
        var result = await updater.UpdateAsync(settings.To, Path.Combine(settings.ResultsDirectory, "coverage"), cancellationToken);
        OutputFormatter.WriteMessage(format, $"Coverage maps: {result.Written.Count} written, {result.Deleted.Count} deleted, {result.FailedProjects.Count} left alone because their tests failed.");
        return result.FailedProjects.Count == 0 ? ExitCodes.Success : ExitCodes.ServerError;
    }
}
