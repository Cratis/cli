// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Runs the Screenplay (.play) files in a folder in a local Stage sandbox using Docker.
/// </summary>
[LlmDescription("Runs the current folder's Screenplay (.play) files in a local Stage sandbox via Docker. Errors if no .play files are present. The Stage API (default port 9090) and the Chronicle Workbench (default port 35000) are published on the host. The container's output is hidden while it starts; progress is reported until the Stage answers, and the command keeps running until stopped.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("run", "Run the Screenplay (.play) files in the current folder in a local Stage sandbox")]
[CliExample("run")]
[CliExample("run", "./screenplays")]
[CliExample("run", "--port", "9191")]
[LlmOption("--tag", "string", "The cratis/stage image tag to run (default: the Stage version this CLI renders with).")]
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

        // The session outlives the startup, so a request to stop - Ctrl+C, a plain kill, a closed terminal -
        // has to shut the container down rather than terminate the command while the sandbox is still running.
        using var interrupt = ShutdownSignal.LinkedTo(cancellationToken);

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
            // A Ctrl+C from a terminal reaches the Docker client too, and stops and removes the container on
            // its own more often than not - but a plain kill only reaches this process, and even a Ctrl+C races
            // the client's own teardown, so what follows confirms the container rather than assuming it.
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
        // A Ctrl+C from a terminal reaches the Docker client too, so give that a moment to take the container
        // down on its own before asking Docker to do it.
        await Exits(session, _stopGrace);

        // Asked unconditionally, because the client exiting does not mean the container did. It is a different
        // process from the container it started, and it can go away while the sandbox keeps running - which is
        // how a run could report a clean stop and leave a container up, holding the port the next run wants.
        // Stopping one that is already gone is not an error.
        await session.Stop();

        return await StoppedWithin(session, _stopTimeout - _stopGrace);
    }

    /// <summary>
    /// Waits until Docker says the container is gone.
    /// </summary>
    /// <param name="session">The session to wait on.</param>
    /// <param name="within">How long to wait.</param>
    /// <returns>True when the container went away in time.</returns>
    static async Task<bool> StoppedWithin(StageSession session, TimeSpan within)
    {
        var deadline = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(deadline) < within)
        {
            if (!await session.IsRunning())
            {
                return true;
            }

            await Task.Delay(StageReadiness.PollInterval, CancellationToken.None);
        }

        return !await session.IsRunning();
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
