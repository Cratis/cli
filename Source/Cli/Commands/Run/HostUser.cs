// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The user and group a container runs as, so it can read the owner-only files a run mounts into it.
/// </summary>
/// <param name="Uid">The user id.</param>
/// <param name="Gid">The group id.</param>
public sealed record HostUser(string Uid, string Gid)
{
    /// <summary>
    /// Gets the <c language="shell">--user</c> value.
    /// </summary>
    public string Argument => $"{Uid}:{Gid}";

    /// <summary>
    /// Finds the current user. Only Linux needs it: there a bind mount keeps the host owner and a file only its owner may read cannot be read
    /// by a container running as someone else. Docker Desktop maps ownership itself.
    /// </summary>
    /// <returns>The user, or null when it is not needed or cannot be found.</returns>
    public static HostUser? Current()
    {
        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        var uid = Ask("-u");
        var gid = Ask("-g");
        return uid is not null && gid is not null ? new(uid, gid) : null;
    }

    static string? Ask(string option)
    {
        try
        {
            var startInfo = new ProcessStartInfo("id", option) { RedirectStandardOutput = true, UseShellExecute = false };
            using var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit();
            return process?.ExitCode == 0 && !string.IsNullOrEmpty(output) && output.All(char.IsAsciiDigit) ? output : null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }
}
