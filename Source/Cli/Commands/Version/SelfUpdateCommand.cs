// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Version;

/// <summary>
/// Updates the Cratis CLI to the latest (or a specific) version using dotnet tool update.
/// </summary>
[CliCommand("update", "Update the Cratis CLI to the latest version")]
[CliExample("update")]
[CliExample("update", "--version", "1.2.3")]
[LlmOutputAdvice("json", "JSON contains previousVersion, currentVersion, and updated flag.")]
[LlmOption("--version", "string", "Specific version to install (default: latest)")]
public class SelfUpdateCommand : AsyncCommand<SelfUpdateSettings>
{
    const string PackageId = "Cratis.Cli";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, SelfUpdateSettings settings, CancellationToken cancellationToken)
    {
        var format = ResolveFormat(settings.Output);
        var currentVersion = VersionCommand.GetCliVersion();

        var arguments = $"tool update -g {PackageId}";
        if (!string.IsNullOrWhiteSpace(settings.TargetVersion))
        {
            arguments += $" --version {settings.TargetVersion}";
        }

        if (string.Equals(format, OutputFormats.Table, StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine($"[bold]Updating Cratis CLI...[/] (current: {currentVersion.EscapeMarkup()})");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            OutputFormatter.WriteError(format, "Failed to start dotnet process", "Ensure the .NET SDK is installed and 'dotnet' is on your PATH", ExitCodes.ServerErrorCode);
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

        var newVersion = VersionCommand.GetCliVersion();

        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            OutputFormatter.WriteObject(format, new
            {
                PreviousVersion = currentVersion,
                CurrentVersion = newVersion,
                Updated = currentVersion != newVersion
            });
        }
        else if (currentVersion != newVersion)
        {
            AnsiConsole.MarkupLine($"[green]Updated from {currentVersion.EscapeMarkup()} to {newVersion.EscapeMarkup()}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Already at the latest version ({currentVersion.EscapeMarkup()})[/]");
        }

        return ExitCodes.Success;
    }

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
