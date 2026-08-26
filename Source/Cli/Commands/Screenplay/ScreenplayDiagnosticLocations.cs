// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Resolves relocation-safe logical locations for Screenplay diagnostics.
/// </summary>
static class ScreenplayDiagnosticLocations
{
    const string TargetSentinel = "<target>";
    const string SourceSentinel = "<source>";

    /// <summary>
    /// Gets the logical target identity without exposing its physical directory.
    /// </summary>
    /// <param name="targetPath">The physical or logical target path.</param>
    /// <returns>The target filename, or a safe sentinel when no filename can be represented.</returns>
    internal static string Target(string? targetPath) => FileNameOr(targetPath, TargetSentinel);

    /// <summary>
    /// Gets the logical filename of a workspace project without exposing its physical directory.
    /// </summary>
    /// <param name="project">The workspace project.</param>
    /// <returns>The project filename, name, or a safe sentinel.</returns>
    internal static string WorkspaceProject(Project project) =>
        FileNameOr(project.FilePath, FileNameOr(project.Name, SourceSentinel));

    /// <summary>
    /// Gets the logical source location for a Roslyn compilation diagnostic.
    /// </summary>
    /// <param name="loaded">The aligned loaded compilation and source contexts.</param>
    /// <param name="projectIndex">The compilation index.</param>
    /// <param name="diagnostic">The Roslyn diagnostic.</param>
    /// <returns>A display path, stable file identity, or safe logical project identity.</returns>
    internal static string CompilationSource(
        LoadedCompilation loaded,
        int projectIndex,
        Diagnostic diagnostic)
    {
        if (projectIndex >= 0 && projectIndex < loaded.ProjectSources.Count)
        {
            var projectSource = loaded.ProjectSources[projectIndex];
            if (diagnostic.Location.SourceTree is { } sourceTree &&
                projectSource.SourceContext.Files.TryGetValue(sourceTree, out var sourceFile))
            {
                return SourceFile(sourceFile);
            }

            return LogicalProject(projectSource);
        }

        return projectIndex >= 0 && projectIndex < loaded.ProjectNames.Count
            ? FileNameOr(loaded.ProjectNames[projectIndex], SourceSentinel)
            : SourceSentinel;
    }

    static string LogicalProject(ScreenplayProjectSource projectSource) =>
        !string.IsNullOrWhiteSpace(projectSource.SourceContext.ProjectIdentity)
            ? projectSource.SourceContext.ProjectIdentity
            : FileNameOr(projectSource.LogicalProjectPath, SourceSentinel);

    static string SourceFile(DotNetSourceFile sourceFile)
    {
        if (!string.IsNullOrWhiteSpace(sourceFile.DisplayPath))
        {
            return sourceFile.DisplayPath;
        }

        return !string.IsNullOrWhiteSpace(sourceFile.Identity.Project) &&
               !string.IsNullOrWhiteSpace(sourceFile.Identity.Path)
            ? $"{sourceFile.Identity.Project}/{sourceFile.Identity.Path}"
            : SourceSentinel;
    }

    static string FileNameOr(string? path, string sentinel)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return sentinel;
        }

        string fileName;
        try
        {
            fileName = Path.GetFileName(path.Replace('\\', '/'));
        }
        catch (ArgumentException)
        {
            return sentinel;
        }

        return string.IsNullOrWhiteSpace(fileName) ||
               string.Equals(fileName, ".", StringComparison.Ordinal) ||
               string.Equals(fileName, "..", StringComparison.Ordinal) ||
               fileName.Any(char.IsControl)
            ? sentinel
            : fileName;
    }
}
