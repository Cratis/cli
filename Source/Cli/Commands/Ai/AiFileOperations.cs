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
