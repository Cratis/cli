// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// A running Stage container — owns the Docker process, keeps what it writes out of the way and tracks its startup.
/// </summary>
public sealed class StageSession : IDisposable
{
    readonly Process _process;
    readonly string _name;

    StageSession(Process process, string name, StageStartup startup, StageOutput output)
    {
        _process = process;
        _name = name;
        Startup = startup;
        Output = output;
    }

    /// <summary>
    /// Gets the startup the container's output is interpreted into.
    /// </summary>
    public StageStartup Startup { get; }

    /// <summary>
    /// Gets the captured output of the container. Empty when the container's output is streamed instead of captured.
    /// </summary>
    public StageOutput Output { get; }

    /// <summary>
    /// Gets the exit code of the container, which is only meaningful once it has exited.
    /// </summary>
    public int ExitCode => _process.ExitCode;

    /// <summary>
    /// Starts the Stage container.
    /// </summary>
    /// <param name="name">The name the container is given, which it can later be stopped by.</param>
    /// <param name="image">The image reference being run, reported while Docker pulls it.</param>
    /// <param name="arguments">The arguments to invoke <c>docker</c> with.</param>
    /// <param name="captureOutput">True to capture the container's output rather than letting it stream to the console.</param>
    /// <returns>The started <see cref="StageSession"/>, or null when the process could not be started.</returns>
    /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the <c>docker</c> executable is not on the PATH.</exception>
    public static StageSession? Start(string name, string image, IReadOnlyList<string> arguments, bool captureOutput)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var startup = new StageStartup(image);
        var output = new StageOutput();
        var process = Process.Start(startInfo);

        if (process is null)
        {
            return null;
        }

        if (captureOutput)
        {
            process.OutputDataReceived += (_, args) => Capture(args.Data);
            process.ErrorDataReceived += (_, args) => Capture(args.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        return new(process, name, startup, output);

        void Capture(string? line)
        {
            if (line is null)
            {
                return;
            }

            output.Append(line);
            startup.Observe(line);
        }
    }

    /// <summary>
    /// Waits until the Stage API answers and the container has finished registering its read models.
    /// </summary>
    /// <param name="port">The host port the Stage API is published on.</param>
    /// <param name="onProgress">Called on every poll so the caller can report progress.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the wait.</param>
    /// <returns>True when the Stage became ready; false when the container exited before that.</returns>
    public async Task<bool> WaitUntilReady(int port, Action onProgress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = StageReadiness.ProbeTimeout };
        long? servingSince = null;

        while (!_process.HasExited)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (servingSince is null && await StageReadiness.IsServing(client, port, cancellationToken))
            {
                servingSince = Stopwatch.GetTimestamp();
            }

            // The API answers a few seconds before the read models are registered with Chronicle, so hold back
            // until the container reports the registration is done - falling through after a grace period so a
            // change to what the container reports delays readiness rather than never resolving it.
            if (servingSince is { } since &&
                (Startup.Phase == StagePhase.Running || Stopwatch.GetElapsedTime(since) > StageReadiness.ReadModelRegistrationGrace))
            {
                return true;
            }

            onProgress();
            await Task.Delay(StageReadiness.PollInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for the container to exit.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the wait.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task WaitForExit(CancellationToken cancellationToken) => _process.WaitForExitAsync(cancellationToken);

    /// <summary>
    /// Asks Docker to stop the container. Only needed when the interrupt did not reach the Docker client itself,
    /// which is the case whenever the command is signalled directly rather than from a terminal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Stop()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in StageContainer.BuildStopArguments(_name))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var stop = Process.Start(startInfo);
        if (stop is null)
        {
            return;
        }

        // Drained rather than shown - by this point the container is expected to be going away anyway, and
        // "no such container" is a perfectly good outcome.
        await stop.StandardOutput.ReadToEndAsync();
        await stop.StandardError.ReadToEndAsync();
        await stop.WaitForExitAsync();
    }

    /// <inheritdoc/>
    public void Dispose() => _process.Dispose();
}
