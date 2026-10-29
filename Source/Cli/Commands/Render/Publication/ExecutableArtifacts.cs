// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Marks published wrapper scripts executable, so a rendered ./mvnw verify runs without chmod.
/// </summary>
/// <remarks>
/// The artifact contract carries no executable flag yet, so scripts are recognized by name: mvnw,
/// gradlew and any *.sh file. Execute permission is added for the owner, and for group and others
/// only where they can already read the file, so the umask the file was created under is respected.
/// </remarks>
internal static class ExecutableArtifacts
{
    static readonly string[] _wrapperNames = ["mvnw", "gradlew"];

    /// <summary>
    /// Determines whether an artifact path names a script that must be executable.
    /// </summary>
    /// <param name="relativePath">The destination-relative artifact path.</param>
    /// <returns>True when the artifact is a wrapper or shell script.</returns>
    public static bool IsScript(string relativePath)
    {
        var name = Path.GetFileName(relativePath.Replace('\\', '/'));
        return _wrapperNames.Contains(name, StringComparer.Ordinal) ||
            name.EndsWith(".sh", StringComparison.Ordinal);
    }

    /// <summary>
    /// Adds execute permission to a published script. Does nothing on Windows or for other artifacts.
    /// </summary>
    /// <param name="path">The published file.</param>
    /// <param name="relativePath">The destination-relative artifact path.</param>
    public static void Apply(string path, string relativePath)
    {
        if (OperatingSystem.IsWindows() || !IsScript(relativePath))
        {
            return;
        }

        var mode = File.GetUnixFileMode(path);
        var executable = mode | UnixFileMode.UserExecute;
        if (mode.HasFlag(UnixFileMode.GroupRead))
        {
            executable |= UnixFileMode.GroupExecute;
        }

        if (mode.HasFlag(UnixFileMode.OtherRead))
        {
            executable |= UnixFileMode.OtherExecute;
        }

        if (executable != mode)
        {
            File.SetUnixFileMode(path, executable);
        }
    }
}
