// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;
using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.New;
using Cratis.Cli.Commands.Run;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Cli.Commands.Version;

return await RunInteractiveCli(args);

static async Task<int> RunInteractiveCli(string[] args)
{
    var protocol = ScreenplayMcpInvocation.IsProtocolRun(args);
    var currentVersion = VersionCommand.GetCliVersion();

    // The request carries its own five second timeout. A deadline measured from here would instead be spent while
    // the command runs, so anything slower than that - the workbench, a run, generating a screenplay - would cancel
    // the check before it ever finished, leaving both the hint and the cached answer permanently out of reach.
    var completing = args.Length > 0 && string.Equals(args[0], "_complete", StringComparison.OrdinalIgnoreCase);

    // The refreshes these checks leave running behind a cached answer are waited for below, and only these. Begin
    // also makes the set reachable for the command that runs below, which can start a check of its own.
    var refreshes = VersionRefreshes.Begin();
    var updateCheckTask = protocol || completing ? Task.FromResult<string?>(null) : UpdateChecker.CheckForUpdate(currentVersion, refreshes);

    // Only reports anything when a Stage image is already on this computer - most commands never touch Docker
    // at all, and a check that mentioned a multi-hundred-megabyte image nobody asked for would be noise, not a hint.
    var stageImageCheckTask = protocol ? Task.FromResult<string?>(null) : StageImageUpdate.CheckForUpdate(false, refreshes);

    // Only reports anything when this directory has the Cratis AI corpus installed, and never touches the network
    // when the hint could not be shown anyway.
    var showsAiHint = !protocol && !completing && !ShouldSkipUpdateHint(args) && AiUpdateCheck.AppliesTo(args) && !Console.IsOutputRedirected && !GlobalSettings.IsAiAgentEnvironment();
    var aiUpdateCheckTask = showsAiHint
        ? AiUpdateCheck.CheckForUpdate(Directory.GetCurrentDirectory(), refreshes)
        : Task.FromResult<AiCorpusUpdate?>(null);

    if (args.Length == 0 && !Console.IsOutputRedirected && !GlobalSettings.IsAiAgentEnvironment())
    {
        Banner.Render();
        FirstRunDetector.ShowIfNeeded();

        // Show static context status so the user immediately sees where the CLI is pointed.
        // This reads from config only — no connection attempt, instant output.
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        var server = ctx.Server ?? "chronicle://localhost:35000";
        var muted = OutputFormatter.Muted.ToMarkup();
        var accent = OutputFormatter.Accent.ToMarkup();
        AnsiConsole.MarkupLine($"  [{muted}]Context:[/] [{accent}]{config.ActiveContextName.EscapeMarkup()}[/] [{muted}]→[/] {server.EscapeMarkup()}");
        AnsiConsole.WriteLine();
    }

    // The CLI framework silently discards options it does not recognize, which would swallow
    // template parameters (--Framework, --Database, ...) before the binder sees them. For the new
    // command, capture them first and hand them to the template parameter binder instead.
    var forwardedArgs = args.Length > 0 && args[0] == "new"
        ? NewCommandArguments.Partition(args)
        : args;
    var console = protocol
        ? AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) })
        : null;
    var exitCode = await CliApp.Create(console).RunAsync(forwardedArgs);

    if (!protocol && !completing && !ShouldSkipUpdateHint(args) &&
        !Console.IsOutputRedirected &&
        !GlobalSettings.IsAiAgentEnvironment())
    {
        // Most commands finish faster than the checks, so give them one short, shared grace window to catch up
        // rather than only showing a hint when its check happens to have finished already. The same window lets
        // a refresh started behind a cached answer record its result - including one a check starts only after
        // the command has finished; anything still running after it is cut off.
        await CachedVersionCheck.WhenSettled([updateCheckTask, stageImageCheckTask, aiUpdateCheckTask], Task.Delay(300), refreshes);

        var strategy = CliUpdate.DetectStrategy();
        ShowHint(updateCheckTask, latestVersion => CliUpdate.GetUpdateHint(strategy, currentVersion, latestVersion));
        ShowHint(stageImageCheckTask, latestStageVersion => $"Stage image update available: {latestStageVersion} - run 'cratis update'");
        ShowHint(aiUpdateCheckTask, AiUpdateCheck.GetUpdateHint);
    }

    return exitCode;
}

// A check that has not finished, or failed, shows nothing: a missing hint never fails the command it follows.
static void ShowHint<T>(Task<T?> check, Func<T, string> hint)
    where T : class
{
    if (check.IsCompletedSuccessfully && check.Result is { } result)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]\u2191 {hint(result).EscapeMarkup()}[/]");
    }
}

static bool ShouldSkipUpdateHint(string[] args) =>
    args.Length > 0 && (string.Equals(args[0], "update", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[0], "version", StringComparison.OrdinalIgnoreCase));

