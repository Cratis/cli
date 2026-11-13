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

    StageSession(Process process, string name, StageStartup startup, StageOutput output)
    {
        _process = process;
        Name = name;
        Startup = startup;
        Output = output;
    }

    /// <summary>
    /// Gets the name the container was given, which it can be stopped by.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the startup the container's output is interpreted into.
    /// </summary>
    public StageStartup Startup { get; }

    /// <summary>
    /// Gets the captured output of the container. Empty when the container's output is streamed instead of captured.
    /// </summary>
    public StageOutput Output { get; }

    /// <summary>
    /// Gets the latest runtime admission status, or null for older images without the status contract.
    /// </summary>
    public StageRuntimeStatus? RuntimeStatus { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the Docker client of the container has exited.
    /// </summary>
    public bool HasExited => _process.HasExited;

    /// <summary>
    /// Gets the exit code of the container, which is only meaningful once it has exited.
    /// </summary>
    public int ExitCode => _process.ExitCode;

    /// <summary>
    /// Starts the Stage container.
    /// </summary>
    /// <param name="name">The name the container is given, which it can later be stopped by.</param>
    /// <param name="image">The image reference being run, reported while Docker pulls it.</param>
    /// <param name="arguments">The arguments to invoke <c language="csharp">docker</c> with.</param>
    /// <param name="captureOutput">True to capture the container's output rather than letting it stream to the console.</param>
    /// <param name="echoPrefix">When set, the captured output is also written to the console, each line led by this prefix. For one container among several, whose output would otherwise be indistinguishable.</param>
    /// <returns>The started <see cref="StageSession"/>, or null when the process could not be started.</returns>
    /// <exception cref="Win32Exception">Thrown when the <c language="csharp">docker</c> executable is not on the PATH.</exception>
    public static StageSession? Start(string name, string image, IReadOnlyList<string> arguments, bool captureOutput, string? echoPrefix = null)
    {
        var startup = new StageStartup(image);
        var output = new StageOutput();
        var process = StartDocker(arguments, captureOutput, Capture);

        return process is null ? null : new(process, name, startup, output);

        void Capture(string? line)
        {
            if (line is null)
            {
                return;
            }

            output.Append(line);
            startup.Observe(line);
            if (echoPrefix is not null)
            {
                Console.WriteLine($"{echoPrefix} {line}");
            }
        }
    }

    /// <summary>
    /// Starts the Stage container in the background and follows what it writes.
    /// </summary>
    /// <param name="name">The name the container is given, which it can later be stopped by.</param>
    /// <param name="image">The image reference being run, reported while Docker pulls it.</param>
    /// <param name="arguments">The arguments to invoke <c language="csharp">docker run -d</c> with.</param>
    /// <param name="captureOutput">True to capture the container's output rather than letting it stream to the console.</param>
    /// <returns>The session, or null when <c language="csharp">docker</c> could not be started.</returns>
    /// <exception cref="Win32Exception">Thrown when the <c language="csharp">docker</c> executable is not on the PATH.</exception>
    /// <remarks>
    /// Docker is asked to create the container and the command waits for it, which is also where an image is pulled.
    /// When Docker refuses, the session is the finished <c language="csharp">docker run</c> with its error as the output, so a
    /// container that never started is reported like one that stopped before it was ready. Otherwise the session
    /// follows the container's logs, which end when the container is gone.
    /// </remarks>
    public static async Task<StageSession?> StartDetached(string name, string image, IReadOnlyList<string> arguments, bool captureOutput)
    {
        var startup = new StageStartup(image);
        var output = new StageOutput();
        var create = StartDocker(arguments, true, Capture);
        if (create is null) return null;

        await create.WaitForExitAsync(CancellationToken.None);
        if (create.ExitCode != 0) return new(create, name, startup, output);

        create.Dispose();
        var follow = StartDocker(StageContainer.BuildLogsArguments(name), captureOutput, Capture);
        return follow is null ? null : new(follow, name, startup, output);

        void Capture(string? line)
        {
            if (line is null) return;
            output.Append(line);
            startup.Observe(line);
        }
    }

    /// <summary>
    /// Stops following the container without stopping it, so it keeps running after this command ends.
    /// </summary>
    public void Detach()
    {
        if (!_process.HasExited) _process.Kill();
    }

    /// <summary>
    /// Waits until the Stage API answers and runtime admission finishes, also waiting for registration progress unless the semantic engine reports ready.
    /// </summary>
    /// <param name="port">The host port the Stage API is published on.</param>
    /// <param name="onProgress">Called on every poll so the caller can report progress.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for canceling the wait.</param>
    /// <returns>True when the Stage became ready; false when it refused the model or the container exited before that.</returns>
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

            if (servingSince is { } since)
            {
                RuntimeStatus = await StageReadiness.ReadStatus(client, port, cancellationToken);
                if (RuntimeStatus is { IsUnsupported: true }) return false;
                var ready = string.Equals(RuntimeStatus?.State, "ready", StringComparison.Ordinal);
                if (ready && string.Equals(RuntimeStatus?.Engine, "semantic", StringComparison.Ordinal)) return true;

                // The semantic host reports ready only after registration/rebuild. EventModel reports ready while
                // its registrar is still running, so retain the registration/grace gate for it and older images.
                // A host explicitly reporting warm or loading must never become ready through this grace period.
                if ((RuntimeStatus is null || ready) &&
                    (Startup.Phase == StagePhase.Running || Stopwatch.GetElapsedTime(since) > StageReadiness.ReadModelRegistrationGrace))
                {
                    return true;
                }
            }

            onProgress();
            await Task.Delay(StageReadiness.PollInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for the container to exit.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for canceling the wait.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task WaitForExit(CancellationToken cancellationToken) => _process.WaitForExitAsync(cancellationToken);

    /// <summary>
    /// Asks Docker to stop the container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Safe to call when the container is already gone - "no such container" is a perfectly good outcome, which
    /// is what makes it safe to ask unconditionally rather than only when something concluded it was needed.
    /// </remarks>
    public async Task Stop() => await DockerCli.Run(StageContainer.BuildStopArguments(Name));

    /// <summary>
    /// Asks Docker whether the container is still running.
    /// </summary>
    /// <returns>True when it is.</returns>
    /// <remarks>
    /// The Docker client this session started is a different process from the container it asked for, and it can
    /// exit while the container keeps running. Watching the client therefore answers a different question than
    /// the one worth asking, which is whether the sandbox is still up.
    /// </remarks>
    public async Task<bool> IsRunning() => StageContainer.IsRunningFrom(await DockerCli.Run(StageContainer.BuildIsRunningArguments(Name)));

    /// <inheritdoc/>
    public void Dispose() => _process.Dispose();

    static Process? StartDocker(IReadOnlyList<string> arguments, bool captureOutput, Action<string?> capture)
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

        var process = Process.Start(startInfo);
        if (process is null || !captureOutput) return process;

        process.OutputDataReceived += (_, args) => capture(args.Data);
        process.ErrorDataReceived += (_, args) => capture(args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }
}
