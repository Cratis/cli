// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected;

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Decides which test projects and tests a change can reach, and writes the selection report.
/// </summary>
[LlmDescription("Computes which test projects and which tests a change can reach, from the project graph and the coverage maps in .cratis/affected/maps. Writes selection.json and selection.md. Any doubt resolves to running more, never less. Does not run tests.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("select", "Select the tests a change can reach", Branch = typeof(AffectedBranch))]
[CliExample("affected", "select")]
[CliExample("affected", "select", "--from", "origin/main", "--level", "project")]
[CliExample("affected", "select", "--mode", "merge")]
[LlmOutputAdvice("json", "One entry per test project with its mode (All, Filtered or Skip), selected and total unit counts, reasons and the runner arguments.")]
[LlmOption("--from", "string", "Ref the change is measured against (default origin/main)")]
[LlmOption("--to", "string", "Ref being tested (default HEAD)")]
[LlmOption("--mode", "string", "pr, merge or nightly (default pr)")]
[LlmOption("--level", "string", "project or test (default test)")]
[LlmOption("--run-all", "bool", "Force a full run")]
[LlmOption("--no-discovery", "bool", "Skip building test projects to discover new tests")]
[LlmOption("--framework", "string", "Target framework to build")]
[LlmOption("--configuration", "string", "Build configuration (default Release)")]
[LlmOption("--report-directory", "string", "Where the report is written (default affected-report)")]
public sealed class AffectedSelectCommand : AsyncCommand<AffectedSelectSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, AffectedSelectSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var git = GitRepository.Open(Directory.GetCurrentDirectory());
            var affectedConfiguration = AffectedConfiguration.Load(git.Root);
            var planner = new AffectedPlanner(git, new XunitV3Runner(git.Root, settings.Configuration, settings.Framework, affectedConfiguration.BuildArguments, affectedConfiguration.ImplicitReferencesOf));
            var report = await planner.PlanAsync(settings.ToPlanOptions(), cancellationToken);
            AffectedReports.Write(report, settings.ReportDirectory);
            AffectedReports.WriteConsole(format, report);
            return ExitCodes.Success;
        }
        catch (Exception exception) when (exception is AffectedException or ArgumentException)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }
}
