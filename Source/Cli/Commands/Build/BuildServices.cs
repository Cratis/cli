// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The state of a project's builder service.
/// </summary>
/// <param name="Name">The service's name.</param>
/// <param name="Manager">The service manager: launchd or systemd.</param>
/// <param name="Unit">The file the service is described in.</param>
/// <param name="Installed">Whether that file exists.</param>
/// <param name="Running">Whether the service manager reports it running.</param>
/// <param name="Logs">Where its output goes.</param>
public sealed record BuildServiceState(string Name, string Manager, string Unit, bool Installed, bool Running, string Logs);

/// <summary>
/// A command of the service manager that failed.
/// </summary>
/// <param name="step">The command.</param>
/// <param name="result">What it answered.</param>
public sealed class BuildServiceFailed(BuildServiceStep step, BuildProcessResult result)
    : Exception($"'{step}' exited with {result.ExitCode}: {(result.Error.Trim().Length > 0 ? result.Error.Trim() : result.Output.Trim())}");

/// <summary>
/// Installs, removes and reports the user service that keeps a builder running.
/// </summary>
/// <param name="platform">The service manager.</param>
/// <param name="run">Runs one of its commands.</param>
public sealed class BuildServices(IBuildServicePlatform platform, Func<BuildServiceStep, CancellationToken, Task<BuildProcessResult>> run)
{
    /// <summary>
    /// Writes the service and starts it, now and at every sign-in. Installing again replaces it.
    /// </summary>
    /// <param name="definition">The service.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Its state.</returns>
    public async Task<BuildServiceState> Install(BuildServiceDefinition definition, CancellationToken cancellationToken)
    {
        var unit = platform.UnitPath(definition.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(unit)!);
        var logs = platform.Logs(definition.Name);
        if (Path.IsPathRooted(logs)) Directory.CreateDirectory(Path.GetDirectoryName(logs)!);
        await File.WriteAllTextAsync(unit, platform.Render(definition), cancellationToken);
        await RunAll(platform.Start(definition.Name), cancellationToken);
        return await Status(definition.Name, cancellationToken);
    }

    /// <summary>
    /// Stops the service and removes it.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Whether there was a service to remove.</returns>
    public async Task<bool> Uninstall(string name, CancellationToken cancellationToken)
    {
        var unit = platform.UnitPath(name);
        if (!File.Exists(unit)) return false;
        await RunAll(platform.Stop(name), cancellationToken);
        File.Delete(unit);
        await RunAll(platform.Forget(name), cancellationToken);
        return true;
    }

    /// <summary>
    /// Reports whether the service is installed and running.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Its state.</returns>
    public async Task<BuildServiceState> Status(string name, CancellationToken cancellationToken)
    {
        var unit = platform.UnitPath(name);
        var installed = File.Exists(unit);
        var running = installed && platform.IsRunning(await run(platform.Query(name), cancellationToken));
        return new(name, platform.Kind, unit, installed, running, platform.Logs(name));
    }

    async Task RunAll(IEnumerable<BuildServiceStep> steps, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            var result = await run(step, cancellationToken);
            if (result.ExitCode != 0 && !step.MayFail) throw new BuildServiceFailed(step, result);
        }
    }
}
