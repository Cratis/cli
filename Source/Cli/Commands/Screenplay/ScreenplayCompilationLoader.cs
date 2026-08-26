// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Loads a solution or project into the Roslyn compilations the Screenplay generator reads.
/// </summary>
/// <remarks>
/// The <c>Cratis.Arc.Screenplay</c> generator deliberately never loads an MSBuild workspace — it takes
/// <see cref="Compilation"/> instances and nothing else. Doing the workspace work here keeps that seam intact and
/// makes the generator equally usable from an MSBuild task, an analyzer, or a spec that builds a compilation from
/// strings.
/// </remarks>
public static class ScreenplayCompilationLoader
{
    static readonly Lock _registration = new();

    /// <summary>
    /// Registers the .NET SDK MSBuild instance with the process.
    /// </summary>
    /// <remarks>
    /// This has to happen before any MSBuild type is touched, which is why every member that does touch one is
    /// marked as not inlinable — the JIT would otherwise resolve those types while this method is still running.
    /// </remarks>
    public static void RegisterMSBuild()
    {
        lock (_registration)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }
    }

    /// <summary>
    /// Loads the given solution or project and returns the compilation to generate from.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<LoadedCompilation> Load(string targetPath, CancellationToken cancellationToken) =>
        Load(targetPath, includeAllProjects: false, targetFramework: null, cancellationToken);

    /// <summary>
    /// Loads the given solution or project and optionally retains every non-spec C# project for provider analysis.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="includeAllProjects">Whether solution projects should bypass Arc-specific artifact filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<LoadedCompilation> Load(
        string targetPath,
        bool includeAllProjects,
        CancellationToken cancellationToken) =>
        Load(targetPath, includeAllProjects, targetFramework: null, cancellationToken);

    /// <summary>
    /// Loads the given solution or project for the requested target framework.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="targetFramework">The target framework to load from multi-targeted projects.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Task<LoadedCompilation> Load(
        string targetPath,
        string? targetFramework,
        CancellationToken cancellationToken) =>
        Load(targetPath, includeAllProjects: false, targetFramework, cancellationToken);

    /// <summary>
    /// Loads the given solution or project for the requested target framework and optionally retains every non-spec
    /// C# project for provider analysis.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="includeAllProjects">Whether solution projects should bypass Arc-specific artifact filtering.</param>
    /// <param name="targetFramework">The target framework to load from multi-targeted projects.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static async Task<LoadedCompilation> Load(
        string targetPath,
        bool includeAllProjects,
        string? targetFramework,
        CancellationToken cancellationToken)
    {
        RegisterMSBuild();
        return await LoadWithWorkspace(targetPath, includeAllProjects, targetFramework, cancellationToken);
    }

    /// <summary>
    /// Creates a stable, non-disclosing diagnostic for an MSBuild workspace failure.
    /// </summary>
    /// <param name="targetLocation">The logical target location.</param>
    /// <returns>The workspace diagnostic.</returns>
    internal static ScreenplayDiagnostic WorkspaceFailure(string targetLocation) =>
        new(
            ScreenplayDiagnosticSeverity.Warning,
            ScreenplayDiagnosticCodes.WorkspaceFailure,
            "MSBuild reported a workspace problem while loading the target",
            targetLocation);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<LoadedCompilation> LoadWithWorkspace(
        string targetPath,
        bool includeAllProjects,
        string? targetFramework,
        CancellationToken cancellationToken)
    {
        var targetLocation = ScreenplayDiagnosticLocations.Target(targetPath);
        var failures = new List<ScreenplayDiagnostic>();
        var failureLock = new Lock();
        using var resources = DesignTimeResourceGeneration.Create();
        using var workspace = MSBuildWorkspace.Create(resources.GlobalProperties);
        using var subscription = workspace.RegisterWorkspaceFailedHandler(_ =>
        {
            lock (failureLock)
            {
                failures.Add(WorkspaceFailure(targetLocation));
            }
        });

        var isSolution = ScreenplayTargetResolver.IsSolution(targetPath);
        IReadOnlyList<Project> selected;
        string workspaceBoundary;
        if (isSolution)
        {
            workspaceBoundary = Path.GetDirectoryName(targetPath)!;
            var candidates = (await workspace.OpenSolutionAsync(targetPath, cancellationToken: cancellationToken)).Projects
                .Where(project => project.Language == LanguageNames.CSharp && !ScreenplayProjectSelection.IsSpecProject(project.Name))
                .ToArray();
            if (candidates.Length == 0)
            {
                return LoadedCompilation.Failed(
                    ScreenplayDiagnosticCodes.NoProject,
                    $"No C# project to generate from was found in '{targetLocation}'",
                    targetLocation,
                    failures);
            }

            var frameworkSelection = ScreenplayWorkspaceProjectSelection.Select(candidates, targetFramework, targetLocation);
            if (frameworkSelection.Diagnostics.Count > 0)
            {
                return new LoadedCompilation(
                    [],
                    [],
                    [.. failures, .. frameworkSelection.Diagnostics]);
            }

            selected = frameworkSelection.Projects;
        }
        else
        {
            var openedRoot = await workspace.OpenProjectAsync(targetPath, cancellationToken: cancellationToken);
            try
            {
                var canonicalTargetPath = ScreenplayProjectSources.CanonicalPathOf(targetPath);
                var rootVariants = openedRoot.Solution.Projects
                    .Where(project =>
                        project.Language == LanguageNames.CSharp &&
                        !ScreenplayProjectSelection.IsSpecProject(project.Name) &&
                        !string.IsNullOrWhiteSpace(project.FilePath) &&
                        ScreenplayProjectSources.PhysicalPathComparer.Equals(
                            ScreenplayProjectSources.CanonicalPathOf(project.FilePath),
                            canonicalTargetPath))
                    .ToArray();
                if (rootVariants.Length == 0)
                {
                    return LoadedCompilation.Failed(
                        ScreenplayDiagnosticCodes.NoProject,
                        $"No C# project to generate from was found in '{targetLocation}'",
                        targetLocation,
                        failures);
                }

                var rootSelection = ScreenplayWorkspaceProjectSelection.Select(rootVariants, targetFramework, targetLocation);
                if (rootSelection.Diagnostics.Count > 0)
                {
                    return new LoadedCompilation(
                        [],
                        [],
                        [.. failures, .. rootSelection.Diagnostics]);
                }

                var closure = ScreenplayDirectProjectSelection.Select(rootSelection.Projects.Single());
                workspaceBoundary = ScreenplayDirectProjectWorkspaceBoundary.Resolve(targetPath, closure);
                selected = ScreenplayDirectProjectSelection.Order(closure, workspaceBoundary);
            }
            catch (InvalidScreenplayProjectSource)
            {
                return LoadedCompilation.Failed(
                    ScreenplayDiagnosticCodes.InvalidSourcePath,
                    "The direct project-reference closure contains a project or source path outside its trusted workspace boundary, or one that cannot be mapped safely",
                    targetLocation,
                    failures);
            }
        }

        var unrestored = selected
            .Where(project => !ProjectRestoreState.IsRestored(project.FilePath, project.CompilationOutputInfo.AssemblyPath))
            .Select(project => ScreenplayProjectSelection.WithoutTargetFramework(project.Name))
            .ToArray();

        if (unrestored.Length > 0)
        {
            return LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.RestoreRequired,
                ProjectRestoreState.MessageFor(unrestored),
                targetLocation,
                failures);
        }

        return await CompilationsOf(selected, isSolution, includeAllProjects, targetPath, workspaceBoundary, failures, cancellationToken);
    }

    /// <summary>
    /// Turns the selected projects into the compilations to generate from.
    /// </summary>
    /// <param name="selected">The projects that take part, ordered by name.</param>
    /// <param name="isSolution">Whether a solution was opened rather than a single project.</param>
    /// <param name="includeAllProjects">Whether all selected projects should bypass Arc-specific artifact filtering.</param>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="workspaceBoundary">The trusted physical boundary used consistently for source identities.</param>
    /// <param name="failures">Everything the workspace reported while loading.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    /// <remarks>
    /// A project of a solution that cannot declare a single artifact is left out silently — a solution regularly
    /// holds an analyzer, a build-time tool or a code-generation project beside the application, and none of them
    /// is anything the reader has to be told about. A project the command was pointed at directly is read whatever
    /// it can see, because pointing at it is the instruction to read it.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<LoadedCompilation> CompilationsOf(
        IReadOnlyList<Project> selected,
        bool isSolution,
        bool includeAllProjects,
        string targetPath,
        string workspaceBoundary,
        IReadOnlyList<ScreenplayDiagnostic> failures,
        CancellationToken cancellationToken)
    {
        var compilations = new List<Compilation>();
        var names = new List<string>();
        var authoredSyntaxTrees = new List<IReadOnlySet<SyntaxTree>>();
        var projectProvenance = new List<ScreenplayProjectProvenance>();
        var projectSources = new List<ScreenplayProjectSource>();
        var usesWorkspaceDisplayRoot = isSolution || selected.Count > 1;

        // A project that yields no compilation is left out of the document rather than ending the run, so that a
        // solution still describes the projects that did load - and is reported as an error, because a document
        // missing part of the application it names is exactly what nobody notices on their own.
        var unloadable = new List<ScreenplayDiagnostic>();

        foreach (var loaded in selected)
        {
            var project = GeneratedResourceSources.AddMissingTo(loaded);
            var name = ScreenplayProjectSelection.WithoutTargetFramework(project.Name);
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                unloadable.Add(new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Error,
                    ScreenplayDiagnosticCodes.NoCompilation,
                    $"No compilation could be created for '{name}', which is therefore not part of the document",
                    ScreenplayDiagnosticLocations.WorkspaceProject(project)));
                continue;
            }

            compilation = ScreenplayFrameworkReferences.AddMissingTo(project, compilation);

            if (isSolution && !includeAllProjects && !ScreenplayProjectSelection.CanDeclareAnArtifact(compilation))
            {
                continue;
            }

            (IReadOnlySet<SyntaxTree> AuthoredSyntaxTrees, ScreenplayProjectSource Source) sourceMapping;
            try
            {
                sourceMapping = await ScreenplayProjectSources.Create(loaded, compilation, workspaceBoundary, usesWorkspaceDisplayRoot, cancellationToken);
            }
            catch (InvalidScreenplayProjectSource)
            {
                return LoadedCompilation.Failed(
                    ScreenplayDiagnosticCodes.InvalidSourcePath,
                    $"Source paths for project '{name}' cannot be mapped to stable portable identities",
                    ScreenplayDiagnosticLocations.Target(targetPath),
                    [.. failures, .. unloadable]);
            }

            var targetFramework = ScreenplayFrameworkReferences.TargetFrameworkOf(project);
            var assetsFile = ProjectRestoreState.AssetsFileFor(project.FilePath, project.CompilationOutputInfo.AssemblyPath);
            var sourcePolicy = sourceMapping.Source.SourceContext.Policy;
            compilations.Add(compilation);
            names.Add(name);
            authoredSyntaxTrees.Add(sourceMapping.AuthoredSyntaxTrees);
            projectSources.Add(sourceMapping.Source);
            projectProvenance.Add(new ScreenplayProjectProvenance(
                name,
                targetFramework,
                ScreenplayPackageProvenance.PackagesFrom(assetsFile, targetFramework),
                ScreenplayPackageProvenance.AssembliesFrom(compilation),
                ScreenplayFrameworkCapabilities.From(compilation))
            {
                SourcePolicy = new ScreenplaySourcePolicyProvenance(
                    sourceMapping.Source.LogicalProjectPath,
                    sourceMapping.Source.SourceContext.ProjectIdentity,
                    sourcePolicy.Version,
                    sourcePolicy.DisplayRoot.ToString(),
                    sourcePolicy.CasePolicy.ToString())
            });
        }

        if (compilations.Count == 0)
        {
            return unloadable.Count == 0
                ? LoadedCompilation.Failed(
                    ScreenplayDiagnosticCodes.NoArtifacts,
                    $"No project in '{ScreenplayDiagnosticLocations.Target(targetPath)}' can declare a command or an event type, so there is nothing to generate a Screenplay from",
                    ScreenplayDiagnosticLocations.Target(targetPath),
                    failures)
                : LoadedCompilation.Failed(
                    ScreenplayDiagnosticCodes.NoCompilation,
                    $"No compilation could be created for any project in '{ScreenplayDiagnosticLocations.Target(targetPath)}'",
                    ScreenplayDiagnosticLocations.Target(targetPath),
                    failures);
        }

        return new LoadedCompilation(compilations, names, [.. failures, .. unloadable])
        {
            AuthoredSyntaxTrees = authoredSyntaxTrees,
            ProjectProvenance = projectProvenance,
            ProjectSources = projectSources
        };
    }
}
