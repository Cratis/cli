// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The operating system's user service manager: launchd on macOS, systemd on Linux.
/// </summary>
public interface IBuildServicePlatform
{
    /// <summary>Gets the name of the service manager.</summary>
    string Kind { get; }

    /// <summary>
    /// Gets the file the service is described in.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The full path.</returns>
    string UnitPath(string name);

    /// <summary>
    /// Gets where the service's output goes, as told to a person.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The log file or the command that shows it.</returns>
    string Logs(string name);

    /// <summary>
    /// Writes the service's description.
    /// </summary>
    /// <param name="definition">The service.</param>
    /// <returns>The content of the unit file.</returns>
    string Render(BuildServiceDefinition definition);

    /// <summary>
    /// Gets the commands that start the service now and at every sign-in, after its file is written.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The commands, in order.</returns>
    IReadOnlyList<BuildServiceStep> Start(string name);

    /// <summary>
    /// Gets the commands that stop the service and keep it from starting again, before its file is removed.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The commands, in order.</returns>
    IReadOnlyList<BuildServiceStep> Stop(string name);

    /// <summary>
    /// Gets the commands that make the service manager forget the service, after its file is removed.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The commands, in order.</returns>
    IReadOnlyList<BuildServiceStep> Forget(string name);

    /// <summary>
    /// Gets the command that asks whether the service runs.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The command.</returns>
    BuildServiceStep Query(string name);

    /// <summary>
    /// Reads the answer of <see cref="Query"/>.
    /// </summary>
    /// <param name="result">What the command answered.</param>
    /// <returns>Whether the service is running.</returns>
    bool IsRunning(BuildProcessResult result);
}
