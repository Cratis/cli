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

    /// <summary>Gets the protected backups to name if a Direct configuration rewrite fails.</summary>
    internal IReadOnlyDictionary<string, string>? ConfigurationBackups { get; init; }

    /// <summary>Gets an optional interleaving seam immediately before the no-follow open.</summary>
    internal Action<string>? BeforeConfigurationOpen { get; init; }

    /// <summary>Gets an optional failing-write seam for recovery diagnostics.</summary>
    internal Action<Stream, string>? WriteConfigurationContent { get; init; }

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
    public void WriteAllTextAtomically(string path, string content, Action? beforeReplace = null) => WriteAllTextAtomicallyCore(path, content, beforeReplace, original: null);

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

    /// <summary>Writes a planned configuration with its original bytes bound to the verified write handle.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="content">The new content.</param>
    /// <param name="beforeReplace">The pathname precondition.</param>
    /// <param name="original">The exact original UTF-8 content, including any BOM.</param>
    internal void WriteAllTextAtomically(string path, string content, Action? beforeReplace, string? original) => WriteAllTextAtomicallyCore(path, content, beforeReplace, original);

    void WriteAllTextAtomicallyCore(string path, string content, Action? beforeReplace, string? original)
    {
        if (DryRun) return;
        if (PreserveConfigurationProtection && File.Exists(path))
        {
            var mayHaveWritten = false;
            try
            {
                if (original is null) throw new IOException("No planned original configuration was supplied.");
                AiConfigurationFile.Write(path, content, System.Text.Encoding.UTF8.GetBytes(original), beforeReplace, BeforeConfigurationOpen, WriteConfigurationContent, () => mayHaveWritten = true);
            }
            catch (IOException error)
            {
                if (mayHaveWritten && ConfigurationBackups?.ContainsKey(path) == true)
                {
                    throw new IOException($"Direct MCP configuration '{path}' may be incomplete. Verify that the path still names the original file and is not a link before restoring the content of backup '{ConfigurationBackups[path]}' into the existing file. {error.Message}", error);
                }
                if (mayHaveWritten) throw;
                throw new IOException($"Direct MCP configuration '{path}' was refused; nothing was written. Review the current file and retry; do not overwrite concurrent changes. {error.Message}", error);
            }
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
                    if (PreserveConfigurationProtection) AiUnixFileAcl.Clear(stream.SafeFileHandle, "new client file");
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
}
