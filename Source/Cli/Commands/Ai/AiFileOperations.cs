// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Performs, or merely reports, the file system changes a corpus synchronization makes.</summary>
/// <remarks>
/// Every mutation in <see cref="AiCorpusSynchronizer"/> goes through one of these so that a dry run has a
/// single place it can be wrong rather than one per call site. A flag threaded through seventeen writes is
/// a flag that eventually misses one, and a dry run that writes is worse than no dry run at all, because
/// it is trusted.
/// </remarks>
/// <param name="DryRun">Whether the operations are reported instead of performed.</param>
public sealed record AiFileOperations(bool DryRun)
{
    /// <summary>Gets operations that actually change the file system.</summary>
    public static AiFileOperations Performing { get; } = new(false);

    /// <summary>Gets whether Direct MCP client writes retain an existing inode and its complete protection.</summary>
    internal bool PreserveConfigurationProtection { get; init; }

    /// <summary>Creates the directory a path sits in.</summary>
    /// <param name="path">The file whose directory is needed.</param>
    public void CreateDirectoryFor(string path)
    {
        if (DryRun) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    /// <summary>Creates a directory.</summary>
    /// <param name="path">The directory to create.</param>
    public void CreateDirectory(string path)
    {
        if (DryRun) return;
        Directory.CreateDirectory(path);
    }

    /// <summary>Writes text to a file.</summary>
    /// <param name="path">The file to write.</param>
    /// <param name="content">The content to write.</param>
    public void WriteAllText(string path, string content)
    {
        if (DryRun) return;
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Writes a managed corpus file whose execute bits follow the corpus without widening other permissions.
    /// </summary>
    /// <remarks>
    /// A managed file is owned by the corpus, so a file that is not a script has its execute bits removed.
    /// Removing one is best effort, since some file systems show every file as executable and refuse to change it.
    /// </remarks>
    /// <param name="path">The file to write.</param>
    /// <param name="content">The managed content to write.</param>
    /// <param name="source">The corpus file whose execute bits are available from the checkout.</param>
    /// <returns><see langword="false"/> when the file needed an execute bit the file system refused to add.</returns>
    public bool WriteCorpusFile(string path, string content, string source)
    {
        WriteAllText(path, content);
        if (DryRun || OperatingSystem.IsWindows()) return true;

        var mode = AiCorpusFileModes.Change(path, content, source);
        if (mode is null) return true;

        var adds = AiCorpusFileModes.Adds(path, mode.Value);
        return AiCorpusFileModes.TryApply(path, mode.Value) || !adds;
    }

    /// <summary>
    /// Atomically replaces a shared configuration file without exposing partially written JSON.
    /// Direct MCP opts into in-place writes after protected backups, retaining an existing file's inode and ACL.
    /// </summary>
    /// <param name="path">The file to replace.</param>
    /// <param name="content">The complete new document.</param>
    /// <param name="beforeReplace">Optional precondition recheck immediately before replacement.</param>
    public void WriteAllTextAtomically(string path, string content, Action? beforeReplace = null)
    {
        if (DryRun) return;
        if (PreserveConfigurationProtection && File.Exists(path))
        {
            beforeReplace?.Invoke();
            using var existing = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            using var replacement = new StreamWriter(existing);
            replacement.Write(content);
            replacement.Flush();
            existing.Flush(true);
            existing.SetLength(existing.Position);
            existing.Flush(true);
            return;
        }
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows() && PreserveConfigurationProtection) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            using (var stream = new FileStream(temporary, options))
            using (var writer = new StreamWriter(stream))
            {
                if (!OperatingSystem.IsWindows())
                {
                    if (PreserveConfigurationProtection) AiUnixFileAcl.Clear(stream.SafeFileHandle);
                    else if (File.Exists(path)) File.SetUnixFileMode(temporary, File.GetUnixFileMode(path));
                }
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }
            beforeReplace?.Invoke();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>Copies a file.</summary>
    /// <param name="source">The file to copy from.</param>
    /// <param name="destination">The file to copy to.</param>
    public void Copy(string source, string destination)
    {
        if (DryRun) return;
        File.Copy(source, destination);
    }

    /// <summary>Deletes a file.</summary>
    /// <param name="path">The file to delete.</param>
    public void DeleteFile(string path)
    {
        if (DryRun) return;
        File.Delete(path);
    }

    /// <summary>Deletes a directory.</summary>
    /// <param name="path">The directory to delete.</param>
    public void DeleteDirectory(string path)
    {
        if (DryRun) return;
        Directory.Delete(path);
    }

    /// <summary>Creates a symbolic link, to either a directory or a file.</summary>
    /// <param name="path">The link to create.</param>
    /// <param name="target">What the link points at.</param>
    /// <param name="isDirectory">Whether the target is a directory.</param>
    public void CreateSymbolicLink(string path, string target, bool isDirectory)
    {
        if (DryRun) return;
        if (isDirectory) Directory.CreateSymbolicLink(path, target);
        else File.CreateSymbolicLink(path, target);
    }
}
