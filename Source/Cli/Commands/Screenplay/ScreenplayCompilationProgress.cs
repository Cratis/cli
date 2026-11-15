// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Owns the progress and cancellation state of a single compilation load.
/// </summary>
internal sealed class ScreenplayCompilationProgress : IDisposable
{
    readonly Action<string>? _reportStep;
    readonly ScreenplayGenerationStage _loading;

    internal ScreenplayCompilationProgress(Action<string>? reportStep, CancellationToken cancellationToken)
    {
        _reportStep = reportStep;
        CancellationToken = cancellationToken;
        _loading = new ScreenplayGenerationStage(ScreenplayGenerationStage.Workspace, reportStep);
    }

    internal CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public void Dispose() => _loading.Dispose();

    internal Task<T> CompleteWorkspace<T>(Func<Task<T>> next)
    {
        CancellationToken.ThrowIfCancellationRequested();
        _loading.Complete();
        return next();
    }

    internal Project PrepareResources(Project loaded)
    {
        CancellationToken.ThrowIfCancellationRequested();
        using var resources = new ScreenplayGenerationStage(ScreenplayGenerationStage.Resources, _reportStep);
        var project = GeneratedResourceSources.AddMissingTo(loaded);
        CancellationToken.ThrowIfCancellationRequested();
        resources.Complete();
        return project;
    }

    internal Task<Compilation?> Compile(Project project) =>
        ScreenplayGenerationStage.Run(
            ScreenplayGenerationStage.Compilation, _reportStep, () => project.GetCompilationAsync(CancellationToken), CancellationToken);
}
