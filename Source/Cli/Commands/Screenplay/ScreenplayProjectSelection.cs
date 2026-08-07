// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Selects which projects of a loaded solution the Screenplay is generated from.
/// </summary>
/// <remarks>
/// A Screenplay describes one application, and an application is regularly split across several projects, so every
/// project that remains takes part in the same document. Whether a project could hold part of it is asked of what it
/// can see rather than of what it is called: every artifact is declared with an attribute the framework ships, so a
/// project resolving neither the Arc nor the Chronicle one cannot declare a single thing the document is made of.
/// <para>
/// Spec projects are turned away separately and by name, because nothing about what a spec project can see tells it
/// apart — it references the same framework the application does, which is the whole point of it. They describe the
/// application rather than being part of it.
/// </para>
/// </remarks>
public static class ScreenplayProjectSelection
{
    static readonly string[] _specNames = ["Specs", "Specifications", "Tests", "Test", "IntegrationTests", "Specs.AppHost"];
    static readonly string[] _artifacts = [WellKnownTypeNames.CommandAttribute, WellKnownTypeNames.EventTypeAttribute];

    /// <summary>
    /// Narrows the projects down to the ones the Screenplay is generated from.
    /// </summary>
    /// <param name="projectNames">The names of the projects found in the solution.</param>
    /// <returns>The remaining project names without their target framework, each one once, ordered by name.</returns>
    public static IReadOnlyList<string> Narrow(IEnumerable<string> projectNames) =>
        [.. projectNames
            .Select(WithoutTargetFramework)
            .Where(name => !IsSpecProject(name))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// Determines whether the given project name identifies a spec or test project.
    /// </summary>
    /// <param name="name">The project name.</param>
    /// <returns><see langword="true"/> when the project holds specs or tests.</returns>
    /// <remarks>
    /// The last segment of the name decides, so a project called <c>Specs</c> is recognized as readily as
    /// <c>MyApp.Specs</c> — a solution that groups its integration specs in a folder regularly names the project
    /// just that, and taking it for part of the application puts test-only artifacts in the document. The host an
    /// integration spec starts the application in is named for the specs it serves and is turned away with them.
    /// </remarks>
    public static bool IsSpecProject(string name)
    {
        var project = WithoutTargetFramework(name);
        return Array.Exists(
            _specNames,
            spec =>
                project.Equals(spec, StringComparison.OrdinalIgnoreCase) ||
                project.EndsWith($".{spec}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Removes the target framework that a multi-targeted project carries in the name a workspace gives it.
    /// </summary>
    /// <param name="name">The project name as the workspace reports it.</param>
    /// <returns>The name of the project itself.</returns>
    /// <remarks>
    /// A workspace opens a multi-targeted project once per target framework and tells the results apart by appending
    /// the framework to the name — <c>MyApp.Specs(net10.0)</c>. Every question asked here is about the project rather
    /// than about one of its target frameworks, and asking them of the decorated name gets both answers wrong: the
    /// spec project is no longer recognized as one, and the several compilations of a single project are taken for
    /// several projects and every one of them is read into the same document.
    /// </remarks>
    public static string WithoutTargetFramework(string name)
    {
        if (!name.EndsWith(')'))
        {
            return name;
        }

        var framework = name.LastIndexOf('(');
        return framework > 0 ? name[..framework] : name;
    }

    /// <summary>
    /// Determines whether a compilation could declare any of what a Screenplay document is made of.
    /// </summary>
    /// <param name="compilation">The compilation to ask.</param>
    /// <returns><see langword="true"/> when it could.</returns>
    /// <remarks>
    /// Every artifact is declared with an attribute the framework ships, so resolving one of those attributes is the
    /// least a project has to be able to do to hold part of the application. A Roslyn analyzer, a build-time tool or
    /// a code-generation project sitting beside the application resolves neither, and reading one in as though it
    /// were part of the application says something about the solution that is not true. What this misses is a
    /// project that can see the framework and declares nothing — a host wiring the application up — which is read
    /// and contributes nothing.
    /// </remarks>
    public static bool CanDeclareAnArtifact(Compilation compilation) =>
        Array.Exists(_artifacts, artifact => compilation.GetTypeByMetadataName(artifact) is not null);
}
