// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Loads a solution or project into the Roslyn compilations the Screenplay generator reads.
/// </summary>
/// <remarks>
/// The <c language="csharp">Cratis.Arc.Screenplay</c> generator deliberately never loads an MSBuild workspace — it takes
/// <see cref="Compilation"/> instances and nothing else. Doing the workspace work here keeps that seam intact and
/// makes the generator equally usable from an MSBuild task, an analyzer, or a spec that builds a compilation from
/// strings.
/// </remarks>
public static class ScreenplayCompilationLoader
{
    /// <summary>
    /// Loads the given solution or project and returns the compilation to generate from.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    public static Task<LoadedCompilation> Load(string targetPath, CancellationToken cancellationToken) =>
        Load(targetPath, includeAllProjects: false, targetFramework: null, cancellationToken);

    /// <summary>
    /// Loads the given solution or project and optionally retains every non-spec C# project for provider analysis.
    /// </summary>
    /// <param name="targetPath">The full path of the solution or project file.</param>
    /// <param name="includeAllProjects">Whether solution projects should bypass Arc-specific artifact filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
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
    internal static Task<LoadedCompilation> Load(
        string targetPath,
        bool includeAllProjects,
        string? targetFramework,
        CancellationToken cancellationToken) =>
        Load(new ScreenplayCompilationRequest(targetPath, includeAllProjects, targetFramework), cancellationToken);

    internal static async Task<LoadedCompilation> Load(
        ScreenplayCompilationRequest request,
        CancellationToken cancellationToken,
        Action<string>? reportStep = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var progress = new ScreenplayCompilationProgress(reportStep, cancellationToken);
        return await LoadWithWorkspace(request, progress);
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

    static async Task<LoadedCompilation> LoadWithWorkspace(
        ScreenplayCompilationRequest request,
        ScreenplayCompilationProgress progress)
    {
        var targetLocation = ScreenplayDiagnosticLocations.Target(request.TargetPath);
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

        var selection = ScreenplayTargetResolver.IsSolution(request.TargetPath)
            ? await SelectSolution(workspace, request, failures, progress)
            : await SelectDirectProject(workspace, request, failures, progress);
        if (selection.Failure is { } failure)
        {
            return failure;
        }

        var unrestored = selection.Projects
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

        return await progress.CompleteWorkspace(() => CompilationsOf(selection, request, failures, progress));
    }

    static async Task<WorkspaceSelection> SelectSolution(
        MSBuildWorkspace workspace,
        ScreenplayCompilationRequest request,
        IReadOnlyList<ScreenplayDiagnostic> failures,
        ScreenplayCompilationProgress progress)
    {
        var boundary = Path.GetDirectoryName(request.TargetPath)!;
        var candidates = (await workspace.OpenSolutionAsync(request.TargetPath, cancellationToken: progress.CancellationToken)).Projects
            .Where(project => project.Language == LanguageNames.CSharp && !ScreenplayProjectSelection.IsSpecProject(project.Name))
            .ToArray();
        var selection = SelectFramework(candidates, request, failures);
        return selection with { Boundary = boundary };
    }

    static async Task<WorkspaceSelection> SelectDirectProject(
        MSBuildWorkspace workspace,
        ScreenplayCompilationRequest request,
        IReadOnlyList<ScreenplayDiagnostic> failures,
        ScreenplayCompilationProgress progress)
    {
        var openedRoot = await workspace.OpenProjectAsync(request.TargetPath, cancellationToken: progress.CancellationToken);
        try
        {
            var rootSelection = SelectFramework(RootVariants(openedRoot, request.TargetPath), request, failures);
            if (rootSelection.Failure is not null)
            {
                return rootSelection;
            }

            var closure = ScreenplayDirectProjectSelection.Select(rootSelection.Projects.Single());
            var boundary = ScreenplayDirectProjectWorkspaceBoundary.Resolve(request.TargetPath, closure);
            return new WorkspaceSelection(ScreenplayDirectProjectSelection.Order(closure, boundary), boundary);
        }
        catch (InvalidScreenplayProjectSource exception)
        {
            return new WorkspaceSelection([], string.Empty, LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.InvalidSourcePath,
                InvalidSourcePathMessage.ForClosure(exception, request.TargetPath),
                ScreenplayDiagnosticLocations.Target(request.TargetPath),
                failures));
        }
    }

    static Project[] RootVariants(Project openedRoot, string targetPath)
    {
        var canonicalTargetPath = ScreenplayProjectSources.CanonicalPathOf(targetPath);
        return [.. openedRoot.Solution.Projects
            .Where(project =>
                project.Language == LanguageNames.CSharp &&
                !ScreenplayProjectSelection.IsSpecProject(project.Name) &&
                !string.IsNullOrWhiteSpace(project.FilePath) &&
                ScreenplayProjectSources.PhysicalPathComparer.Equals(
                    ScreenplayProjectSources.CanonicalPathOf(project.FilePath),
                    canonicalTargetPath))];
    }

    static WorkspaceSelection SelectFramework(
        Project[] candidates,
        ScreenplayCompilationRequest request,
        IReadOnlyList<ScreenplayDiagnostic> failures)
    {
        var targetLocation = ScreenplayDiagnosticLocations.Target(request.TargetPath);
        if (candidates.Length == 0)
        {
            return new WorkspaceSelection([], string.Empty, LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.NoProject,
                $"No C# project to generate from was found in '{targetLocation}'",
                targetLocation,
                failures));
        }

        var selection = ScreenplayWorkspaceProjectSelection.Select(candidates, request.TargetFramework, targetLocation);
        return selection.Diagnostics.Count > 0
            ? new WorkspaceSelection([], string.Empty, new LoadedCompilation([], [], [.. failures, .. selection.Diagnostics]))
            : new WorkspaceSelection(selection.Projects, string.Empty);
    }

    /// <summary>
    /// Turns the selected projects into the compilations to generate from.
    /// </summary>
    /// <param name="selection">The selected projects and their trusted physical boundary.</param>
    /// <param name="request">The target and project filtering options.</param>
    /// <param name="failures">Everything the workspace reported while loading.</param>
    /// <param name="progress">The progress and cancellation state of this load.</param>
    /// <returns>The <see cref="LoadedCompilation"/> describing the outcome.</returns>
    /// <remarks>
    /// A project of a solution that cannot declare a single artifact is left out silently — a solution regularly
    /// holds an analyzer, a build-time tool or a code-generation project beside the application, and none of them
    /// is anything the reader has to be told about. A project the command was pointed at directly is read whatever
    /// it can see, because pointing at it is the instruction to read it.
    /// </remarks>
    static async Task<LoadedCompilation> CompilationsOf(
        WorkspaceSelection selection,
        ScreenplayCompilationRequest request,
        IReadOnlyList<ScreenplayDiagnostic> failures,
        ScreenplayCompilationProgress progress)
    {
        var results = new CompilationResults();
        var usesWorkspaceDisplayRoot = ScreenplayTargetResolver.IsSolution(request.TargetPath) || selection.Projects.Count > 1;

        foreach (var loaded in selection.Projects)
        {
            var project = progress.PrepareResources(loaded);
            var name = ScreenplayProjectSelection.WithoutTargetFramework(project.Name);
            var compilation = await progress.Compile(project);
            if (compilation is null)
            {
                // Keep the projects that did load, but report the missing part of the application as an error.
                results.Unloadable.Add(new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Error,
                    ScreenplayDiagnosticCodes.NoCompilation,
                    $"No compilation could be created for '{name}', which is therefore not part of the document",
                    ScreenplayDiagnosticLocations.WorkspaceProject(project)));
                continue;
            }

            compilation = ScreenplayFrameworkReferences.AddMissingTo(project, compilation);

            if (ShouldFilter(request, compilation))
            {
                continue;
            }

            (IReadOnlySet<SyntaxTree> AuthoredSyntaxTrees, ScreenplayProjectSource Source) sourceMapping;
            try
            {
                sourceMapping = await ScreenplayProjectSources.Create(loaded, compilation, selection.Boundary, usesWorkspaceDisplayRoot, progress.CancellationToken);
            }
            catch (InvalidScreenplayProjectSource exception)
            {
                return LoadedCompilation.Failed(
                    ScreenplayDiagnosticCodes.InvalidSourcePath,
                    InvalidSourcePathMessage.For(name, exception, request.TargetPath),
                    ScreenplayDiagnosticLocations.Target(request.TargetPath),
                    [.. failures, .. results.Unloadable]);
            }

            results.Add(project, compilation, sourceMapping);
        }

        return results.Build(request.TargetPath, failures);
    }

    static bool ShouldFilter(ScreenplayCompilationRequest request, Compilation compilation) =>
        ScreenplayTargetResolver.IsSolution(request.TargetPath) &&
        !request.IncludeAllProjects &&
        !ScreenplayProjectSelection.CanDeclareAnArtifact(compilation);

    sealed record WorkspaceSelection(IReadOnlyList<Project> Projects, string Boundary, LoadedCompilation? Failure = null);

    sealed class CompilationResults
    {
        readonly List<Compilation> _compilations = [];
        readonly List<string> _names = [];
        readonly List<IReadOnlySet<SyntaxTree>> _authoredSyntaxTrees = [];
        readonly List<ScreenplayProjectProvenance> _projectProvenance = [];
        readonly List<ScreenplayProjectSource> _projectSources = [];

        internal List<ScreenplayDiagnostic> Unloadable { get; } = [];

        internal void Add(
            Project project,
            Compilation compilation,
            (IReadOnlySet<SyntaxTree> AuthoredSyntaxTrees, ScreenplayProjectSource Source) sourceMapping)
        {
            var name = ScreenplayProjectSelection.WithoutTargetFramework(project.Name);
            var targetFramework = ScreenplayFrameworkReferences.TargetFrameworkOf(project);
            var assetsFile = ProjectRestoreState.AssetsFileFor(project.FilePath, project.CompilationOutputInfo.AssemblyPath);
            var sourcePolicy = sourceMapping.Source.SourceContext.Policy;
            _compilations.Add(compilation);
            _names.Add(name);
            _authoredSyntaxTrees.Add(sourceMapping.AuthoredSyntaxTrees);
            _projectSources.Add(sourceMapping.Source);
            _projectProvenance.Add(new ScreenplayProjectProvenance(
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

        internal LoadedCompilation Build(string targetPath, IReadOnlyList<ScreenplayDiagnostic> failures)
        {
            if (_compilations.Count == 0)
            {
                return Unloadable.Count == 0
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

            return new LoadedCompilation(_compilations, _names, [.. failures, .. Unloadable])
            {
                AuthoredSyntaxTrees = _authoredSyntaxTrees,
                ProjectProvenance = _projectProvenance,
                ProjectSources = _projectSources
            };
        }
    }
}
