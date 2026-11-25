// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Normalizes the paths found in project files to repository relative paths with forward slashes.
/// </summary>
public static class PathNormalizer
{
    /// <summary>
    /// Gets the directory part of a repository relative path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The directory, empty at the repository root.</returns>
    public static string DirectoryOf(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? string.Empty : path[..index];
    }

    /// <summary>
    /// Resolves a path written in a project file against the directory of that project.
    /// </summary>
    /// <param name="directory">The repository relative directory of the project.</param>
    /// <param name="relative">The path as written, with either kind of slash.</param>
    /// <returns>The repository relative path, or null when it leaves the repository or depends on an MSBuild property.</returns>
    public static string? Resolve(string directory, string relative)
    {
        var text = relative.Trim().Replace('\\', '/')
            .Replace("$(MSBuildThisFileDirectory)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$(MSBuildProjectDirectory)/", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (text.Contains("$(", StringComparison.Ordinal) || text.StartsWith('/'))
        {
            return null;
        }

        var segments = new List<string>(directory.Length == 0 ? [] : directory.Split('/'));
        foreach (var segment in text.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return null;
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }
}
