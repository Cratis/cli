// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Tells which files of a project a step wrote, changed or removed, by comparing what the project held before and after it.
/// Git's own folder and the folders of restored packages and build output are left out.
/// </summary>
public static class ProjectFiles
{
    static readonly HashSet<string> _skipped = [with(StringComparer.Ordinal), ".git", "node_modules", "bin", "obj"];

    /// <summary>
    /// Takes what the project holds: each file by its path relative to the project, with its size and last write.
    /// </summary>
    /// <param name="project">The project folder.</param>
    /// <returns>The snapshot.</returns>
    public static IReadOnlyDictionary<string, (long Length, DateTime Written)> Snapshot(string project)
    {
        var files = new Dictionary<string, (long, DateTime)>(StringComparer.Ordinal);
        if (!Directory.Exists(project)) return files;
        var pending = new Stack<DirectoryInfo>([new DirectoryInfo(project)]);
        while (pending.TryPop(out var folder))
        {
            foreach (var child in folder.EnumerateDirectories().Where(child => !_skipped.Contains(child.Name))) pending.Push(child);
            foreach (var file in folder.EnumerateFiles())
            {
                files[Path.GetRelativePath(project, file.FullName).Replace('\\', '/')] = (file.Length, file.LastWriteTimeUtc);
            }
        }

        return files;
    }

    /// <summary>
    /// Gets the files that were added, changed or removed between two snapshots.
    /// </summary>
    /// <param name="before">The snapshot before.</param>
    /// <param name="after">The snapshot after.</param>
    /// <returns>The paths, relative to the project.</returns>
    public static IEnumerable<string> Changed(
        IReadOnlyDictionary<string, (long Length, DateTime Written)> before,
        IReadOnlyDictionary<string, (long Length, DateTime Written)> after) =>
        after.Where(file => !before.TryGetValue(file.Key, out var earlier) || earlier != file.Value).Select(file => file.Key)
            .Concat(before.Keys.Where(path => !after.ContainsKey(path)));
}
