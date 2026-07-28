// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Loads a solution or project into the Roslyn compilation the Screenplay generator reads.
/// </summary>
/// <remarks>
/// The <c>Cratis.Arc.Screenplay</c> generator deliberately never loads an MSBuild workspace — it takes a
/// <see cref="Compilation"/> and nothing else. Doing the workspace work here keeps that seam intact and makes the
/// generator equally usable from an MSBuild task, an analyzer, or a spec that builds a compilation from strings.
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
        using var workspace = MSBuildWorkspace.Create();
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

        var candidates = byName.Values
            .Select(project => new ScreenplayProjectCandidate(project.Name, IsExecutable(project)))
            .ToArray();

        var narrowed = ScreenplayProjectSelection.Narrow(candidates);
        if (narrowed.Count == 0)
        {
            return LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.NoProject,
                $"No C# project to generate from was found in '{targetPath}'",
                targetPath,
                failures);
        }

        if (narrowed.Count > 1)
        {
            return LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.AmbiguousProject,
                $"'{targetPath}' holds {narrowed.Count} candidate projects ({string.Join(", ", narrowed.Select(candidate => candidate.Name))}) — pass the project to generate from",
                targetPath,
                failures);
        }

        var selected = byName[narrowed[0].Name];
        var compilation = await selected.GetCompilationAsync(cancellationToken);
        return compilation is null
            ? LoadedCompilation.Failed(
                ScreenplayDiagnosticCodes.NoCompilation,
                $"No compilation could be created for '{selected.Name}'",
                selected.FilePath ?? targetPath,
                failures)
            : new LoadedCompilation(compilation, selected.Name, failures);
    }

    static bool IsExecutable(Project project) =>
        project.CompilationOptions?.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;
}
