// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Puts the strongly typed resource classes MSBuild generates into a project's intermediate output folder back into
/// the loaded project when the design-time build left them out.
/// </summary>
/// <remarks>
/// <see cref="DesignTimeResourceGeneration"/> makes the design-time build produce these itself, which also works for
/// a project that has never been built. This is the safety net for when it cannot — a read-only intermediate output
/// folder, or an MSBuild that no longer honours the hook. A previous ordinary build then still left the sources on
/// disk, and reading them back is enough to make the compilation whole. Only files the project does not already
/// compile are added, so this does nothing at all once the design-time build does its job.
/// </remarks>
public static class GeneratedResourceSources
{
    const string Suffix = ".Designer.cs";

    /// <summary>
    /// Adds every generated resource source missing from the project.
    /// </summary>
    /// <param name="project">The project as the workspace loaded it.</param>
    /// <returns>The project, with the missing sources added as documents.</returns>
    public static Project AddMissingTo(Project project)
    {
        var missing = MissingFrom(
            project.CompilationOutputInfo.AssemblyPath,
            project.Documents.Select(document => document.FilePath));

        var solution = project.Solution;
        foreach (var file in missing)
        {
            solution = solution.AddDocument(
                DocumentId.CreateNewId(project.Id),
                Path.GetFileName(file),
                SourceText.From(File.ReadAllText(file)),
                filePath: file);
        }

        return solution.GetProject(project.Id) ?? project;
    }

    /// <summary>
    /// Finds the generated resource sources sitting next to the intermediate assembly that are not compiled already.
    /// </summary>
    /// <param name="intermediateAssemblyPath">The path of the assembly the project compiles into its intermediate output folder.</param>
    /// <param name="compiledPaths">The paths of the files the project already compiles.</param>
    /// <returns>The paths of the missing sources, ordered.</returns>
    public static IReadOnlyList<string> MissingFrom(string? intermediateAssemblyPath, IEnumerable<string?> compiledPaths)
    {
        if (string.IsNullOrWhiteSpace(intermediateAssemblyPath))
        {
            return [];
        }

        var folder = Path.GetDirectoryName(intermediateAssemblyPath);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return [];
        }

        var compiled = compiledPaths
            .Where(path => !string.IsNullOrEmpty(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. Directory
                .EnumerateFiles(folder, $"*{Suffix}", SearchOption.TopDirectoryOnly)
                .Where(file => !compiled.Contains(file))
                .Order(StringComparer.Ordinal)
        ];
    }
}
