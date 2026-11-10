// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Finds the user service manager of this machine and the program a service runs.
/// </summary>
public static class BuildServicePlatforms
{
    /// <summary>
    /// Runs a command of the service manager.
    /// </summary>
    /// <param name="step">The command.</param>
    /// <param name="cancellationToken">Cancels it.</param>
    /// <returns>What it answered.</returns>
    public static Task<BuildProcessResult> Run(BuildServiceStep step, CancellationToken cancellationToken) =>
        BuildProcess.Run(step.Program, step.Arguments, Directory.GetCurrentDirectory(), cancellationToken: cancellationToken);

    /// <summary>
    /// Gets the service manager of this machine.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The service manager, or <see langword="null"/> on an operating system without one this CLI supports.</returns>
    public static async Task<IBuildServicePlatform?> Current(CancellationToken cancellationToken)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsMacOS())
        {
            var id = await Run(new("id", ["-u"]), cancellationToken);
            return id.ExitCode == 0 ? new LaunchdBuildService(home, id.Output.Trim()) : null;
        }

        if (OperatingSystem.IsLinux())
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            return new SystemdBuildService(string.IsNullOrWhiteSpace(configHome) ? Path.Combine(home, ".config") : configHome);
        }

        return null;
    }

    /// <summary>
    /// Gets the program a service starts to run this CLI, and the arguments that come before its own: the path of the
    /// <c language="shell">cratis</c> program, or <c language="shell">dotnet</c> and the CLI's assembly when it runs through the host.
    /// </summary>
    /// <param name="processPath">The path of the running process.</param>
    /// <param name="assemblyPath">The path of the CLI's assembly.</param>
    /// <returns>The program and its leading arguments.</returns>
    public static (string Program, IReadOnlyList<string> Prefix) Program(string processPath, string assemblyPath)
    {
        var name = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase) ? (processPath, [assemblyPath]) : (processPath, []);
    }
}
