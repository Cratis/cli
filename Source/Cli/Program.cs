// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;
using Cratis.Cli.Commands.Version;

using var updateCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var currentVersion = VersionCommand.GetCliVersion();
var updateCheckTask = UpdateChecker.CheckForUpdate(currentVersion, updateCts.Token);

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

var exitCode = await CliApp.Create().RunAsync(args);

if (!ShouldSkipUpdateHint(args) &&
    !Console.IsOutputRedirected &&
    !GlobalSettings.IsAiAgentEnvironment())
{
    try
    {
        // Most commands finish faster than the NuGet check, so give it a short grace
        // window to catch up rather than only showing the hint when it happens to have
        // finished already - otherwise the hint would rarely appear in practice.
        await Task.WhenAny(updateCheckTask, Task.Delay(300));
        if (updateCheckTask.IsCompletedSuccessfully && await updateCheckTask is { } latestVersion)
        {
            var strategy = CliUpdate.DetectStrategy();
            var hint = CliUpdate.GetUpdateHint(strategy, currentVersion, latestVersion);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]\u2191 {hint.EscapeMarkup()}[/]");
        }
    }
    catch
    {
        // Update check failures are non-critical.
    }
}

return exitCode;

static bool ShouldSkipUpdateHint(string[] args) =>
    args.Length > 0 && (string.Equals(args[0], "update", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[0], "version", StringComparison.OrdinalIgnoreCase));
