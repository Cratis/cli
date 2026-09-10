// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Selects the direct project and its transitive C# project-reference closure.
/// </summary>
static class ScreenplayDirectProjectSelection
{
    /// <summary>
    /// Selects retained projects without admitting unrelated, reverse, or specification projects.
    /// </summary>
    /// <param name="rootProject">The exact target-framework variant targeted directly.</param>
    /// <returns>The project-reference closure in traversal order.</returns>
    internal static IReadOnlyList<Project> Select(Project rootProject)
    {
        var selected = new List<Project>();
        var pending = new Queue<Project>();
        var visited = new HashSet<ProjectId>();
        pending.Enqueue(rootProject);

        while (pending.TryDequeue(out var project))
        {
            if (!visited.Add(project.Id) || ScreenplayProjectSelection.IsSpecProject(project.Name))
            {
                continue;
            }

            if (project.Language == LanguageNames.CSharp)
            {
                selected.Add(project);
            }

            foreach (var reference in project.ProjectReferences)
            {
                if (project.Solution.GetProject(reference.ProjectId) is { } dependency)
                {
                    pending.Enqueue(dependency);
                }
            }
        }

        return selected;
    }

    /// <summary>
    /// Orders selected direct-workspace projects by relocation-safe source identity.
    /// </summary>
    /// <param name="projects">The selected target-framework variants.</param>
    /// <param name="workspaceRoot">The physical root used only to derive logical paths.</param>
    /// <returns>The deterministically ordered projects.</returns>
    internal static IReadOnlyList<Project> Order(IEnumerable<Project> projects, string workspaceRoot) =>
    [
        .. projects
            .Select(project => new
            {
                Project = project,
                LogicalPath = ScreenplayProjectSources.RelativeTo(workspaceRoot, ProjectPathOf(project))
            })
            .OrderBy(project => project.LogicalPath, StringComparer.Ordinal)
            .ThenBy(project => ScreenplayProjectSelection.WithoutTargetFramework(project.Project.Name), StringComparer.Ordinal)
            .ThenBy(project => ScreenplayFrameworkReferences.TargetFrameworkOf(project.Project), StringComparer.Ordinal)
            .Select(project => project.Project)
    ];

    static string ProjectPathOf(Project project) =>
        !string.IsNullOrWhiteSpace(project.FilePath) && Path.IsPathFullyQualified(project.FilePath)
            ? project.FilePath
            : throw new InvalidScreenplayProjectSource("A direct project-reference dependency has no fully qualified project path");
}
