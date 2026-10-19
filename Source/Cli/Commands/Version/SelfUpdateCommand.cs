// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Cli.Commands.Run;
using Cratis.Cli.Templates;

namespace Cratis.Cli.Commands.Version;

/// <summary>
/// Updates the Cratis CLI to the latest version using the detected installation method.
/// </summary>
[LlmDescription("Updates the cratis CLI to the latest version using the appropriate installation method (dotnet tool or Homebrew), then installs the latest Cratis templates under ~/.cratis/templates unless --no-templates is given.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("update", "Update the Cratis CLI to the latest version")]
[CliExample("update")]
[CliExample("update", "--version", "1.2.3")]
[CliExample("update", "--no-templates")]
[LlmOutputAdvice("json", "JSON contains previousVersion, currentVersion, updated flag, and templates (previousVersion, currentVersion, updated, message) unless --no-templates is given.")]
[LlmOption("--version", "string", "Specific version to install (default: latest)")]
[LlmOption("--no-templates", "bool", "Skip checking for and installing newer Cratis templates")]
public class SelfUpdateCommand : AsyncCommand<SelfUpdateSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SelfUpdateSettings settings, CancellationToken cancellationToken)
    {
        var format = ResolveFormat(settings.Output);
        var isInteractive = string.Equals(format, OutputFormats.Table, StringComparison.Ordinal);
        var currentVersion = VersionCommand.GetCliVersion();
        var strategy = CliUpdate.DetectStrategy();

        // Check for available updates before performing the update. This is only used to
        // show the user what to expect - the actual outcome is always verified afterward
        // by asking the package manager what got installed, since this check can be stale
        // (cached) or fail outright without that meaning the real update will also fail.
        var expectedNewVersion = settings.TargetVersion;
        if (string.IsNullOrWhiteSpace(expectedNewVersion))
        {
            using var updateCheckCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                // Asking for an update is the user saying the cached answer is not good enough, so this one
                // always goes to the source.
                expectedNewVersion = await RunWithStatus(
                    "Checking for updates...",
                    () => UpdateChecker.CheckForCliUpdate(currentVersion, true, updateCheckCts.Token));
            }
            catch
            {
                // If we can't check for updates, proceed anyway - the update tool will handle it
            }
        }

        if (isInteractive)
        {
            var message = !string.IsNullOrWhiteSpace(expectedNewVersion)
                ? $"[bold]Updating Cratis CLI...[/] ({currentVersion.EscapeMarkup()} -> {expectedNewVersion.EscapeMarkup()})"
                : $"[bold]Updating Cratis CLI...[/] (current: {currentVersion.EscapeMarkup()})";
            AnsiConsole.MarkupLine(message);
        }

        var preUpdateStartInfo = CliUpdate.CreatePreUpdateProcessStartInfo(strategy, settings.TargetVersion);
        if (preUpdateStartInfo is not null)
        {
            var preUpdateResult = await RunWithStatus("Refreshing Homebrew...", () => RunProcess(preUpdateStartInfo));
            if (preUpdateResult is not null)
            {
                return preUpdateResult.Value;
            }
        }

        var startInfo = CliUpdate.CreateUpdateProcessStartInfo(strategy, settings.TargetVersion);
        if (startInfo is null)
        {
            if (!string.IsNullOrWhiteSpace(settings.TargetVersion) && strategy == CliUpdateStrategy.Homebrew)
            {
                OutputFormatter.WriteError(format, "Target version is not supported for Homebrew updates", "Run 'cratis update' to upgrade to the latest Homebrew version", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            var instructions = CliUpdate.GetManualUpdateInstructions(strategy) ?? "Manual update required.";
            var manualStageImageResult = await UpdateStageImageIfPresent();
            var manualTemplatesResult = await RefreshTemplates(currentVersion);
            if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
            {
                OutputFormatter.WriteObject(format, new
                {
                    PreviousVersion = currentVersion,
                    CurrentVersion = currentVersion,
                    Updated = false,
                    Strategy = strategy.ToString(),
                    Message = instructions,
                    StageImage = manualStageImageResult,
                    Templates = manualTemplatesResult
                });
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]{instructions.EscapeMarkup()}[/]");
                WriteStageImageResult(manualStageImageResult);
                WriteTemplatesResult(manualTemplatesResult);
            }
            return ExitCodes.Success;
        }

        var updateResult = await RunWithStatus(
            strategy == CliUpdateStrategy.Homebrew ? "Updating via Homebrew..." : "Updating via dotnet tool...",
            () => RunProcess(startInfo));
        if (updateResult is not null)
        {
            return updateResult.Value;
        }

        // The currently running process won't reflect the new version until it's restarted, so
        // ask the package manager directly what's actually installed now. This is the ground
        // truth - the pre-update check above is only a hint and may not have found anything.
        var installedVersion = await CliUpdate.GetInstalledVersion(strategy, cancellationToken);
        var newVersion = installedVersion ?? expectedNewVersion ?? currentVersion;
        var wasUpdated = !string.Equals(newVersion, currentVersion, StringComparison.OrdinalIgnoreCase);

        var stageImageResult = await UpdateStageImageIfPresent();
        var templatesResult = await RefreshTemplates(newVersion);

        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            OutputFormatter.WriteObject(format, new
            {
                PreviousVersion = currentVersion,
                CurrentVersion = newVersion,
                Updated = wasUpdated,
                StageImage = stageImageResult,
                Templates = templatesResult
            });
        }
        else if (wasUpdated)
        {
            AnsiConsole.MarkupLine($"[green]Updated from {currentVersion.EscapeMarkup()} to {newVersion.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine("[dim]The new version will be active when you run 'cratis' again.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Already at the latest version ({currentVersion.EscapeMarkup()})[/]");
        }

        WriteStageImageResult(stageImageResult);
        WriteTemplatesResult(templatesResult);

        return ExitCodes.Success;

        async Task<int?> RunProcess(ProcessStartInfo processStartInfo)
        {
            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                var hint = strategy switch
                {
                    CliUpdateStrategy.DotNetTool => "Ensure the .NET SDK is installed and 'dotnet' is on your PATH",
                    CliUpdateStrategy.Homebrew => "Ensure Homebrew is installed and 'brew' is on your PATH",
                    _ => null
                };
                OutputFormatter.WriteError(format, "Failed to start update process", hint, ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout.Trim();
                OutputFormatter.WriteError(format, $"Update failed: {errorMessage}", errorCode: ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            }

            return null;
        }

        async Task<T> RunWithStatus<T>(string statusText, Func<Task<T>> action)
        {
            if (!isInteractive)
            {
                return await action();
            }

            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(new Style(OutputFormatter.Accent))
                .StartAsync(statusText, _ => action());
        }

        // Only checked when a Stage image already exists on this computer - most users never run 'cratis run'
        // and pulling a multi-hundred-megabyte image nobody asked for is not what 'cratis update' should do.
        async Task<StageImageUpdateResult?> UpdateStageImageIfPresent()
        {
            try
            {
                return await RunWithStatus("Checking the Stage image...", () => StageImageUpdate.CheckAndUpdate(cancellationToken));
            }
            catch
            {
                return null;
            }
        }

        // Templates are refreshed for the CLI that is installed now, so a set needing the new CLI is accepted.
        // A failed check never fails the update: the installed templates stay in place.
        Task<TemplateRefreshResult?> RefreshTemplates(string cliVersion) =>
            RunWithStatus("Checking for new templates...", () => RefreshTemplatesUnlessSkipped(settings, TemplateInstaller.ForCurrentUser(), cliVersion));

        void WriteTemplatesResult(TemplateRefreshResult? result)
        {
            if (result is null)
            {
                return;
            }

            if (result.Message is not null)
            {
                AnsiConsole.MarkupLine($"[yellow]Templates were not updated: {result.Message.EscapeMarkup()}[/]");
            }
            else if (result.Updated)
            {
                AnsiConsole.MarkupLine(result.PreviousVersion is null
                    ? $"[green]Installed Cratis templates {(result.CurrentVersion ?? string.Empty).EscapeMarkup()}[/]"
                    : $"[green]Updated Cratis templates from {result.PreviousVersion.EscapeMarkup()} to {(result.CurrentVersion ?? string.Empty).EscapeMarkup()}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]The Cratis templates are already at the latest version ({(result.CurrentVersion ?? string.Empty).EscapeMarkup()})[/]");
            }
        }

        void WriteStageImageResult(StageImageUpdateResult? result)
        {
            if (result is null)
            {
                return;
            }

            AnsiConsole.MarkupLine(result.Updated
                ? $"[green]Updated the Stage image from {result.PreviousVersion.EscapeMarkup()} to {result.CurrentVersion.EscapeMarkup()}[/]"
                : $"[green]The Stage image is already at the latest version ({result.PreviousVersion.EscapeMarkup()})[/]");
        }
    }

    /// <summary>
    /// Checks for and installs newer templates as part of an update, unless <c language="bash">--no-templates</c> was given.
    /// </summary>
    /// <param name="settings">The update settings.</param>
    /// <param name="installer">The template installer.</param>
    /// <param name="cliVersion">The CLI version installed now.</param>
    /// <returns>The refresh outcome, or null when the templates were skipped.</returns>
    internal static async Task<TemplateRefreshResult?> RefreshTemplatesUnlessSkipped(SelfUpdateSettings settings, TemplateInstaller installer, string cliVersion) =>
        settings.NoTemplates ? null : await installer.Refresh(cliVersion);

    static string ResolveFormat(string output)
    {
        if (string.Equals(output, OutputFormats.JsonCompact, StringComparison.OrdinalIgnoreCase))
        {
            return OutputFormats.JsonCompact;
        }

        if (!string.Equals(output, OutputFormats.Auto, StringComparison.OrdinalIgnoreCase))
        {
            return output.ToLowerInvariant();
        }

        if (GlobalSettings.IsAiAgentEnvironment())
        {
            return OutputFormats.JsonCompact;
        }

        return Console.IsOutputRedirected ? OutputFormats.Json : OutputFormats.Table;
    }
}
