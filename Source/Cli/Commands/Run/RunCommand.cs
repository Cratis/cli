// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Runs the Screenplay (.play) files in a folder in a local Stage sandbox using Docker.
/// </summary>
[LlmDescription("Runs the current folder's Screenplay (.play) files in a local Stage sandbox via Docker. Errors if no .play files are present. The Stage API (default port 9090) and the Chronicle Workbench (default port 35000) are published on the host. The container's output is hidden while it starts; progress is reported until the Stage answers, and the command keeps running until stopped.")]
[CliCommand("run", "Run the Screenplay (.play) files in the current folder in a local Stage sandbox")]
[CliExample("run")]
[CliExample("run", "./screenplays")]
[CliExample("run", "--port", "9191")]
[LlmOption("--tag", "string", "The cratis/stage image tag to run (default: latest).")]
[LlmOption("--port", "int", "Host port to publish the Stage API on (default: 9090).")]
[LlmOption("--workbench-port", "int", "Host port to publish the Chronicle Workbench on (default: 35000).")]
[LlmOption("--verbose", "bool", "Stream the container's output instead of showing startup progress.")]
public class RunCommand : AsyncCommand<RunSettings>
{
    /// <summary>
    /// How long to wait for the container to go away on its own after the command is interrupted, before
    /// stopping it explicitly.
    /// </summary>
    static readonly TimeSpan _stopGrace = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long to give Docker to stop and remove the container after the command is interrupted.
    /// </summary>
    static readonly TimeSpan _stopTimeout = TimeSpan.FromSeconds(30);

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

        var endpoints = StageEndpoints.For(settings.Port, settings.WorkbenchPort);
        RunOutput.WriteHeader(format, path, endpoints);

        using var session = Start(path, settings);
        if (session is null)
        {
            OutputFormatter.WriteError(format, "Failed to start Docker", "Ensure Docker is installed and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        // The session outlives the startup, so Ctrl+C has to shut the container down rather than terminate
        // the command while the sandbox is still running.
        using var interrupt = ConsoleInterrupt.LinkedTo(cancellationToken);

        try
        {
            if (settings.Verbose)
            {
                await session.WaitForExit(interrupt.Token);
                return ExitCodeFor(session);
            }

            if (!await WaitUntilReady(session, settings, format, interrupt.Token))
            {
                // The container exited on its own - wait for the process so all of its output is captured,
                // then show it, since it was hidden while starting.
                await session.WaitForExit(CancellationToken.None);
                RunOutput.WriteFailure(format, session);
                return ExitCodes.ServerError;
            }

            RunOutput.WriteReady(format, session.Startup, path, endpoints);
            await session.WaitForExit(interrupt.Token);

            return ExitCodeFor(session);
        }
        catch (OperationCanceledException)
        {
            // A Ctrl+C from a terminal reaches the Docker client too, which stops and removes the container on
            // its own. Wait for that rather than returning while the sandbox is still being torn down.
            RunOutput.WriteStopping(format);
            if (!await WaitForStop(session))
            {
                RunOutput.WriteStopTimedOut(format, _stopTimeout);
            }

            return ExitCodes.Success;
        }
    }

    static StageSession? Start(string path, RunSettings settings)
    {
        var name = StageContainer.GenerateName();
        var arguments = StageContainer.BuildRunArguments(path, settings.Tag, settings.Port, settings.WorkbenchPort, name);

        try
        {
            return StageSession.Start(name, $"{StageContainer.Image}:{settings.Tag}", arguments, captureOutput: !settings.Verbose);
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    static async Task<bool> WaitUntilReady(StageSession session, RunSettings settings, string format, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        if (!string.Equals(format, OutputFormats.Table, StringComparison.Ordinal))
        {
            return await session.WaitUntilReady(settings.Port, () => { }, cancellationToken);
        }

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(OutputFormatter.Accent))
            .StartAsync(
                RunOutput.StatusText(session.Startup, TimeSpan.Zero),
                async ctx => await session.WaitUntilReady(
                    settings.Port,
                    () => ctx.Status(RunOutput.StatusText(session.Startup, Stopwatch.GetElapsedTime(started))),
                    cancellationToken));
    }

    static async Task<bool> WaitForStop(StageSession session)
    {
        if (await Exits(session, _stopGrace))
        {
            return true;
        }

        // Nothing signalled Docker itself - which is the case whenever the command is signalled directly rather
        // than from a terminal - so the sandbox is still up and has to be stopped explicitly.
        await session.Stop();

        return await Exits(session, _stopTimeout - _stopGrace);
    }

    static async Task<bool> Exits(StageSession session, TimeSpan within)
    {
        try
        {
            await session.WaitForExit(CancellationToken.None).WaitAsync(within, CancellationToken.None);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    static int ExitCodeFor(StageSession session) => session.ExitCode == 0 ? ExitCodes.Success : ExitCodes.ServerError;
}
