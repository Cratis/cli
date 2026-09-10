// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Resolves the trusted workspace boundary for a directly targeted project closure.
/// </summary>
static class ScreenplayDirectProjectWorkspaceBoundary
{
    /// <summary>
    /// Resolves one canonical boundary that contains every retained project.
    /// </summary>
    /// <param name="targetProjectPath">The directly targeted project file.</param>
    /// <param name="projects">The exact retained project-reference closure.</param>
    /// <returns>The canonical trusted repository or common workspace boundary.</returns>
    /// <exception cref="InvalidScreenplayProjectSource">Thrown when no safe boundary can be established.</exception>
    internal static string Resolve(string targetProjectPath, IReadOnlyList<Project> projects)
    {
        var canonicalTargetPath = CanonicalProjectPathOf(targetProjectPath);
        var targetDirectory = Path.GetDirectoryName(canonicalTargetPath) ??
            throw new InvalidScreenplayProjectSource("The directly targeted project has no containing directory");
        var projectDirectories = projects
            .Select(project => Path.GetDirectoryName(CanonicalProjectPathOf(project.FilePath)) ??
                throw new InvalidScreenplayProjectSource("A retained project has no containing directory"))
            .ToArray();

        var boundary = NearestGitRootOf(targetDirectory) ?? SafeCommonAncestorOf(projectDirectories);
        foreach (var project in projects)
        {
            ScreenplayProjectSources.RelativeTo(boundary, CanonicalProjectPathOf(project.FilePath));
        }

        return boundary;
    }

    static string? NearestGitRootOf(string targetDirectory)
    {
        for (var candidate = targetDirectory; candidate is not null; candidate = Directory.GetParent(candidate)?.FullName)
        {
            var marker = Path.Combine(candidate, ".git");
            if (Directory.Exists(marker) || File.Exists(marker))
            {
                return ScreenplayProjectSources.CanonicalPathOf(candidate);
            }
        }

        return null;
    }

    static string SafeCommonAncestorOf(string[] directories)
    {
        if (directories.Length == 0)
        {
            throw new InvalidScreenplayProjectSource("The direct project-reference closure has no project directories");
        }

        var candidate = directories[0];
        while (directories.Any(directory => !Contains(candidate, directory)))
        {
            candidate = Directory.GetParent(candidate)?.FullName ??
                throw new InvalidScreenplayProjectSource("The direct project-reference closure has no common workspace boundary");
        }

        var fileSystemRoot = Path.GetPathRoot(candidate);
        if (string.IsNullOrWhiteSpace(candidate) ||
            string.IsNullOrWhiteSpace(fileSystemRoot) ||
            string.Equals(
                Path.TrimEndingDirectorySeparator(candidate),
                Path.TrimEndingDirectorySeparator(ScreenplayProjectSources.CanonicalPathOf(fileSystemRoot)),
                StringComparison.Ordinal))
        {
            throw new InvalidScreenplayProjectSource("The direct project-reference closure would broaden the workspace boundary to the filesystem root");
        }

        return candidate;
    }

    static bool Contains(string boundary, string path)
    {
        var relative = Path.GetRelativePath(boundary, path).Replace('\\', '/');
        return string.Equals(relative, ".", StringComparison.Ordinal) ||
            (!Path.IsPathFullyQualified(relative) &&
             !string.Equals(relative, "..", StringComparison.Ordinal) &&
             !relative.StartsWith("../", StringComparison.Ordinal));
    }

    static string CanonicalProjectPathOf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            throw new InvalidScreenplayProjectSource("A direct project-reference dependency has no fully qualified project path");
        }

        return ScreenplayProjectSources.CanonicalPathOf(path);
    }
}
