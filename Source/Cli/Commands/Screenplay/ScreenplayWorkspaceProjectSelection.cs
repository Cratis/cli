// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Selects one target-framework variant of each distinct workspace project without conflating duplicate display names.
/// </summary>
static class ScreenplayWorkspaceProjectSelection
{
    /// <summary>
    /// Selects workspace projects by their project-file identity and requested target framework.
    /// </summary>
    /// <param name="candidates">The workspace projects to select from.</param>
    /// <param name="requestedFramework">The requested target framework, if any.</param>
    /// <param name="targetPath">The target path used as a diagnostic location.</param>
    /// <returns>The selected projects and target-framework diagnostics.</returns>
    internal static (IReadOnlyList<Project> Projects, IReadOnlyList<ScreenplayDiagnostic> Diagnostics) Select(
        IEnumerable<Project> candidates,
        string? requestedFramework,
        string targetPath)
    {
        var selected = new List<Project>();
        var diagnostics = new List<ScreenplayDiagnostic>();
        var groups = candidates
            .GroupBy(ProjectIdentity, ScreenplayProjectSources.PhysicalPathComparer)
            .OrderBy(group => ScreenplayProjectSelection.WithoutTargetFramework(group.First().Name), StringComparer.Ordinal)
            .ThenBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var variants = group.OrderBy(project => project.Name, StringComparer.Ordinal).ToArray();
            var selection = ScreenplayTargetFrameworkSelector.Select(
                variants.Select(project => project.Name),
                requestedFramework,
                targetPath);
            diagnostics.AddRange(selection.Diagnostics);
            if (selection.IsSuccessful)
            {
                selected.AddRange(selection.ProjectNames.Select(name => variants.Single(project => string.Equals(project.Name, name, StringComparison.Ordinal))));
            }
        }

        return (selected, diagnostics);
    }

    /// <summary>
    /// Gets the physical project-file identity used only while the workspace remains loaded.
    /// </summary>
    /// <param name="project">The workspace project.</param>
    /// <returns>The physical project identity.</returns>
    static string ProjectIdentity(Project project) => project.FilePath ?? project.Id.Id.ToString("D");
}
