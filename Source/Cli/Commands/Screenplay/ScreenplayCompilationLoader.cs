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
    public static async Task<LoadedCompilation> Load(string targetPath, CancellationToken cancellationToken)
    {
        RegisterMSBuild();
        return await LoadWithWorkspace(targetPath, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<LoadedCompilation> LoadWithWorkspace(string targetPath, CancellationToken cancellationToken)
    {
        var failures = new List<ScreenplayDiagnostic>();
        var failureLock = new Lock();
        using var resources = DesignTimeResourceGeneration.Create();
        using var workspace = MSBuildWorkspace.Create(resources.GlobalProperties);
        using var subscription = workspace.RegisterWorkspaceFailedHandler(args =>
        {
            lock (failureLock)
            {
                failures.Add(new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Warning,
                    ScreenplayDiagnosticCodes.WorkspaceFailure,
                    args.Diagnostic.Message,
                    targetPath));
            }
        });

        var projects = ScreenplayTargetResolver.IsSolution(targetPath)
            ? (await workspace.OpenSolutionAsync(targetPath, cancellationToken: cancellationToken)).Projects
            : [await workspace.OpenProjectAsync(targetPath, cancellationToken: cancellationToken)];

        var byName = projects
            .Where(project => project.Language == LanguageNames.CSharp)
            .GroupBy(project => project.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var narrowed = ScreenplayProjectSelection.Narrow(byName.Keys);
        if (narrowed.Count == 0)
        {
            return LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.NoProject,
                $"No C# project to generate from was found in '{targetPath}'",
                targetPath,
                failures);
        }

        var compilations = new List<Compilation>();
        var names = new List<string>();

        // A project that yields no compilation is left out of the document rather than ending the run, so that a
        // solution still describes the projects that did load - and is reported as an error, because a document
        // missing part of the application it names is exactly what nobody notices on their own.
        var unloadable = new List<ScreenplayDiagnostic>();

        foreach (var name in narrowed)
        {
            var project = GeneratedResourceSources.AddMissingTo(byName[name]);
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                unloadable.Add(new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Error,
                    ScreenplayDiagnosticCodes.NoCompilation,
                    $"No compilation could be created for '{project.Name}', which is therefore not part of the document",
                    project.FilePath ?? targetPath));
                continue;
            }

            compilations.Add(compilation);
            names.Add(project.Name);
        }

        if (compilations.Count == 0)
        {
            return LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.NoCompilation,
                $"No compilation could be created for any project in '{targetPath}'",
                targetPath,
                failures);
        }

        return new LoadedCompilation(compilations, names, [.. failures, .. unloadable]);
    }
}
