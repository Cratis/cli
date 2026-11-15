// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The part of the Stage session that starts a container in the background and lets go of it again.
/// </summary>
public sealed partial class StageSession
{
    /// <summary>
    /// Starts the Stage container in the background and follows what it writes.
    /// </summary>
    /// <param name="name">The name the container is given, which it can later be stopped by.</param>
    /// <param name="image">The image reference being run, reported while Docker pulls it.</param>
    /// <param name="arguments">The arguments to invoke <c language="csharp">docker run -d</c> with.</param>
    /// <param name="captureOutput">True to capture the container's output rather than letting it stream to the console.</param>
    /// <param name="cancellationToken">A token that abandons the start, which also stops any container it created.</param>
    /// <returns>The session, or null when <c language="csharp">docker</c> could not be started.</returns>
    /// <exception cref="Win32Exception">Thrown when the <c language="csharp">docker</c> executable is not on the PATH.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the start was abandoned through <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// Docker is asked to create the container and the command waits for it, which is also where an image is pulled.
    /// When Docker refuses, the session is the finished <c language="csharp">docker run</c> with its error as the output, so a
    /// container that never started is reported like one that stopped before it was ready. Otherwise the session
    /// follows the container's logs, which end when the container is gone. A start that ends without a session, by
    /// failure or cancellation, stops the container it may have created, so none is left running.
    /// </remarks>
    public static async Task<StageSession?> StartDetached(string name, string image, IReadOnlyList<string> arguments, bool captureOutput, CancellationToken cancellationToken)
    {
        var startup = new StageStartup(image);
        var output = new StageOutput();
        var create = StartDocker(arguments, true, Capture);
        if (create is null) return null;

        StageSession? session = null;
        try
        {
            await create.WaitForExitAsync(cancellationToken);
            if (create.ExitCode != 0) return session = new(create, name, startup, output);

            session = Follow(name, captureOutput, startup, output);
            if (session is not null) create.Dispose();
            return session;
        }
        finally
        {
            if (session is null) await Abandon(create, name);
        }

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

    static StageSession? Follow(string name, bool captureOutput, StageStartup startup, StageOutput output)
    {
        var follow = StartDocker(
            StageContainer.BuildLogsArguments(name),
            captureOutput,
            line =>
            {
                if (line is null) return;
                output.Append(line);
                startup.Observe(line);
            });
        return follow is null ? null : new(follow, name, startup, output);
    }

    static async Task Abandon(Process create, string name)
    {
        if (!create.HasExited) create.Kill();
        create.Dispose();
        await DockerCli.Run(StageContainer.BuildStopArguments(name));
    }
}
