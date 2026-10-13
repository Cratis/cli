// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Decides the execute bits a managed corpus file should have, and whether they can be applied.
/// </summary>
/// <remarks>
/// A managed file is owned by the corpus, so it is executable exactly when the corpus file is executable or
/// starts with a shebang. Execute is only granted to the permission classes that can already read the file,
/// so nothing becomes more widely accessible than it was.
/// </remarks>
public static class AiCorpusFileModes
{
    const UnixFileMode ExecuteBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    /// <summary>
    /// Gets the mode an existing managed file should have, when it differs from its current mode.
    /// </summary>
    /// <param name="path">The installed file.</param>
    /// <param name="content">The managed content written to it.</param>
    /// <param name="source">The corpus file it comes from.</param>
    /// <returns>The mode to apply, or <see langword="null"/> when the file is missing, already correct, or on Windows.</returns>
    public static UnixFileMode? Change(string path, string content, string source)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(path)) return null;

        var executable = (File.GetUnixFileMode(source) & ExecuteBits) != UnixFileMode.None || content.StartsWith("#!", StringComparison.Ordinal);
        var current = File.GetUnixFileMode(path);
        var target = executable ? current | ReadableExecuteBits(current) : current & ~ExecuteBits;
        return target == current ? null : target;
    }

    /// <summary>
    /// Checks whether applying a mode would grant an execute bit the file does not have.
    /// </summary>
    /// <param name="path">The installed file.</param>
    /// <param name="target">The mode to apply.</param>
    /// <returns><see langword="true"/> when the mode adds an execute bit.</returns>
    /// <remarks>
    /// Only an addition must succeed: a missing execute bit breaks a script, while a stale one on a
    /// non-script is harmless and some file systems show every file as executable and refuse to change it.
    /// </remarks>
    public static bool Adds(string path, UnixFileMode target) => !OperatingSystem.IsWindows() && (target & ~File.GetUnixFileMode(path) & ExecuteBits) != UnixFileMode.None;

    /// <summary>
    /// Applies a mode, reporting a refusal instead of throwing.
    /// </summary>
    /// <param name="path">The installed file.</param>
    /// <param name="target">The mode to apply.</param>
    /// <returns><see langword="true"/> when the mode was applied.</returns>
    public static bool TryApply(string path, UnixFileMode target)
    {
        if (OperatingSystem.IsWindows()) return true;

        try
        {
            File.SetUnixFileMode(path, target);

            // Some file systems accept a mode change and ignore it, so success is what the file reports afterwards.
            return File.GetUnixFileMode(path) == target;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            // Refused by the owner check or the file system: the caller decides whether that matters.
            return false;
        }
    }

    /// <summary>
    /// Checks whether the current user may change an existing file's mode.
    /// </summary>
    /// <param name="path">The installed file.</param>
    /// <returns><see langword="true"/> when the mode can be changed.</returns>
    /// <remarks>
    /// Only the owner may change a mode, and some file systems refuse it outright. Reapplying the current
    /// mode needs the same permission as changing it, so it probes the refusal before any file is written.
    /// The probe is a real mode change to the same value and updates the file's change time, so a dry run
    /// must not call it.
    /// </remarks>
    public static bool CanChange(string path) => OperatingSystem.IsWindows() || TryApply(path, File.GetUnixFileMode(path));

    static UnixFileMode ReadableExecuteBits(UnixFileMode mode)
    {
        var bits = UnixFileMode.None;
        if (mode.HasFlag(UnixFileMode.UserRead)) bits |= UnixFileMode.UserExecute;
        if (mode.HasFlag(UnixFileMode.GroupRead)) bits |= UnixFileMode.GroupExecute;
        if (mode.HasFlag(UnixFileMode.OtherRead)) bits |= UnixFileMode.OtherExecute;
        return bits;
    }
}
