// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Runs the Screenplay (.play) files in a folder in a local Stage sandbox using Docker.
/// </summary>
[LlmDescription("Runs the current folder's Screenplay (.play) files in a local Stage sandbox via Docker. Errors if no .play files are present. The Stage API is published on the host (default port 9090).")]
[CliCommand("run", "Run the Screenplay (.play) files in the current folder in a local Stage sandbox")]
[CliExample("run")]
[CliExample("run", "./screenplays")]
[CliExample("run", "--port", "9191")]
[LlmOption("--tag", "string", "The cratis/stage image tag to run (default: latest).")]
[LlmOption("--port", "int", "Host port to publish the Stage API on (default: 9090).")]
public class RunCommand : AsyncCommand<RunSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var path = Path.GetFullPath(settings.Path ?? Directory.GetCurrentDirectory());

        if (!Directory.Exists(path))
        {
            OutputFormatter.WriteError(format, $"Folder '{path}' does not exist", "Run this command from a folder that contains one or more .play files, or pass the path to one", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        if (!PlayFiles.ExistIn(path))
        {
            OutputFormatter.WriteError(format, "No Screenplay files (.play) found in the folder", "Run this command from a folder that contains one or more .play files, or pass the path to one", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var arguments = StageContainer.BuildRunArguments(path, settings.Tag, settings.Port);
        var startInfo = new ProcessStartInfo { FileName = "docker" };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal) &&
            !string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) &&
            !string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Starting Stage from {path.EscapeMarkup()} on http://localhost:{settings.Port}[/]");
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Win32Exception)
        {
            OutputFormatter.WriteError(format, "Failed to start Docker", "Ensure Docker is installed and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        if (process is null)
        {
            OutputFormatter.WriteError(format, "Failed to start Docker", "Ensure Docker is installed and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode == 0 ? ExitCodes.Success : ExitCodes.ServerError;
    }
}
