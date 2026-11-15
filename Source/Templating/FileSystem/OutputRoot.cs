// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.FileSystem;

/// <summary>
/// Keeps template-supplied paths inside the output root. A path is contained when, after normalization and
/// after resolving every symbolic link along it, it is the root itself or a descendant of it.
/// </summary>
public static class OutputRoot
{
    static readonly StringComparison _comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Resolves a template-supplied path against the output root when it stays inside it.
    /// </summary>
    /// <param name="outputRoot">The output root.</param>
    /// <param name="path">A path relative to the output root. Absolute paths are always refused.</param>
    /// <param name="fullPath">The normalized full path when contained.</param>
    /// <returns>True when the path is relative and stays inside the output root.</returns>
    public static bool TryResolve(string outputRoot, string path, out string fullPath)
    {
        fullPath = Path.GetFullPath(Path.Combine(Path.GetFullPath(outputRoot), path));
        return !Path.IsPathRooted(path) && Contains(outputRoot, fullPath);
    }

    /// <summary>
    /// Tells whether a full path stays inside the output root, following symbolic links on both.
    /// </summary>
    /// <param name="outputRoot">The output root.</param>
    /// <param name="fullPath">The path to check.</param>
    /// <returns>True when the path is the root or a descendant of it.</returns>
    public static bool Contains(string outputRoot, string fullPath)
    {
        var root = Path.GetFullPath(outputRoot);
        var path = Path.GetFullPath(fullPath);
        return IsWithin(root, path) && IsWithin(RealPath(root), RealPath(path));
    }

    /// <summary>
    /// Resolves a template-supplied path against the output root, refusing one that leaves it.
    /// </summary>
    /// <param name="outputRoot">The output root.</param>
    /// <param name="path">A path relative to the output root.</param>
    /// <returns>The normalized full path.</returns>
    /// <exception cref="InvalidTemplateManifest">The path leaves the output root.</exception>
    public static string Resolve(string outputRoot, string path) =>
        TryResolve(outputRoot, path, out var fullPath)
            ? fullPath
            : throw new InvalidTemplateManifest($"path '{path}' resolves outside the output directory '{outputRoot}'.");

    static bool IsWithin(string root, string path)
    {
        var trimmedRoot = Path.TrimEndingDirectorySeparator(root);
        var trimmedPath = Path.TrimEndingDirectorySeparator(path);
        if (trimmedPath.Equals(trimmedRoot, _comparison)) return true;
        return trimmedPath.StartsWith(Path.EndsInDirectorySeparator(trimmedRoot) ? trimmedRoot : trimmedRoot + Path.DirectorySeparatorChar, _comparison);
    }

    static string RealPath(string fullPath)
    {
        var root = Path.GetPathRoot(fullPath) ?? string.Empty;
        var current = root;
        foreach (var part in fullPath[root.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (info.LinkTarget is null) continue;
            var target = info.ResolveLinkTarget(returnFinalTarget: true);
            if (target is not null) current = RealPath(target.FullName);
        }

        return current;
    }
}
