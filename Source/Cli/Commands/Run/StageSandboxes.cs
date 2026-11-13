// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Finds and stops the Stage sandboxes <c language="csharp">cratis run</c> started, by the name each was given.
/// </summary>
public static class StageSandboxes
{
    /// <summary>
    /// Lists the names of the running sandboxes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The names, in the order Docker reports them; empty when none run or Docker is not available.</returns>
    public static async Task<IReadOnlyList<string>> Running(CancellationToken cancellationToken = default)
    {
        var output = await DockerCli.Run(StageContainer.BuildListArguments(), cancellationToken);
        return [.. output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => name.StartsWith(StageContainer.NamePrefix, StringComparison.Ordinal))];
    }

    /// <summary>
    /// Asks Docker to stop a sandbox and waits for it to be gone.
    /// </summary>
    /// <param name="name">The name of the sandbox.</param>
    /// <param name="within">How long to wait for it to go away.</param>
    /// <returns>True when the sandbox went away in time.</returns>
    public static async Task<bool> Stop(string name, TimeSpan within)
    {
        await DockerCli.Run(StageContainer.BuildStopArguments(name));
        return await StoppedWithin(name, within);
    }

    /// <summary>
    /// Waits until Docker says the sandbox is gone.
    /// </summary>
    /// <param name="name">The name of the sandbox.</param>
    /// <param name="within">How long to wait.</param>
    /// <returns>True when the sandbox went away in time.</returns>
    public static async Task<bool> StoppedWithin(string name, TimeSpan within)
    {
        var deadline = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(deadline) < within)
        {
            if (!await IsRunning(name))
            {
                return true;
            }

            await Task.Delay(StageReadiness.PollInterval, CancellationToken.None);
        }

        return !await IsRunning(name);
    }

    static async Task<bool> IsRunning(string name) =>
        StageContainer.IsRunningFrom(await DockerCli.Run(StageContainer.BuildIsRunningArguments(name)));
}
