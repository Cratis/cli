// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Removes the directories that removing Cratis-managed files and links left empty.</summary>
internal static class AiEmptyDirectories
{
    /// <summary>
    /// Removes each directory above a removed path that is now empty, stopping at the first one that still holds
    /// anything. Only directories that held a removed path are considered, so nothing outside the managed corpus
    /// and the harness link directories is touched, and neither the project nor the .cratis directory is ever removed.
    /// </summary>
    /// <param name="project">The project root.</param>
    /// <param name="removed">The removed files and links, relative to the project.</param>
    /// <param name="operations">The guarded file operations.</param>
    internal static void Prune(string project, IEnumerable<string> removed, AiFileOperations operations)
    {
        // A dry run removed nothing, so nothing became empty.
        if (operations.DryRun) return;
        foreach (var path in removed)
        {
            try
            {
                for (var directory = Parent(path); directory is not null && IsEmpty(Path.Combine(project, directory)); directory = Parent(directory))
                {
                    operations.DeleteDirectory(Path.Combine(project, directory));
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Pruning is opportunistic; a failed cleanup must not stop synchronization.
            }
        }
    }

    static string? Parent(string path)
    {
        var separator = path.LastIndexOf('/');
        if (separator < 0) return null;
        var parent = path[..separator];
        return parent == ".cratis" ? null : parent;
    }

    static bool IsEmpty(string directory)
    {
        var info = new DirectoryInfo(directory);
        return info.Exists && info.LinkTarget is null && !info.EnumerateFileSystemInfos().Any();
    }
}
