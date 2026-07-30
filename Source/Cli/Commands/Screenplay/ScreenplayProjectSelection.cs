// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Selects which projects of a loaded solution the Screenplay is generated from.
/// </summary>
/// <remarks>
/// A Screenplay describes one application, and an application is regularly split across several projects, so every
/// project that remains takes part in the same document. Spec projects are dropped — they describe the application
/// rather than being part of it.
/// </remarks>
public static class ScreenplayProjectSelection
{
    static readonly string[] _specNames = ["Specs", "Specifications", "Tests", "Test", "IntegrationTests"];

    /// <summary>
    /// Narrows the projects down to the ones the Screenplay is generated from.
    /// </summary>
    /// <param name="projectNames">The names of the projects found in the solution.</param>
    /// <returns>The remaining project names, ordered by name.</returns>
    public static IReadOnlyList<string> Narrow(IEnumerable<string> projectNames) =>
        [.. projectNames
            .Where(name => !IsSpecProject(name))
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// Determines whether the given project name identifies a spec or test project.
    /// </summary>
    /// <param name="name">The project name.</param>
    /// <returns><see langword="true"/> when the project holds specs or tests.</returns>
    /// <remarks>
    /// The last segment of the name decides, so a project called <c>Specs</c> is recognized as readily as
    /// <c>MyApp.Specs</c> — a solution that groups its integration specs in a folder regularly names the project
    /// just that, and taking it for part of the application puts test-only artifacts in the document.
    /// </remarks>
    public static bool IsSpecProject(string name) =>
        Array.Exists(
            _specNames,
            spec =>
                name.Equals(spec, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith($".{spec}", StringComparison.OrdinalIgnoreCase));
}
