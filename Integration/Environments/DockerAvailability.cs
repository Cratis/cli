// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Integration;

/// <summary>
/// Tells whether a Docker daemon can be reached, so container-based specs skip instead of failing where Docker is absent.
/// </summary>
public static class DockerAvailability
{
    static readonly Lazy<bool> _available = new(Probe);

    /// <summary>
    /// Gets a value indicating whether a Docker daemon is reachable.
    /// </summary>
    public static bool IsAvailable => _available.Value;

    static bool Probe()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", "info --format {{.ServerVersion}}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            return process?.WaitForExit(15000) == true && process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
