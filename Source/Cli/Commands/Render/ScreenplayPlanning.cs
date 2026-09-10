// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Compiles one Screenplay document set and plans all target artifacts before publication.
/// </summary>
/// <param name="compiler">The semantic document-set compiler.</param>
/// <param name="targets">The static reviewed renderer-target roster.</param>
internal sealed class ScreenplayPlanning(
    ISemanticModelCompiler compiler,
    RenderTargetRoster targets) : IScreenplayPlanning
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayPlanning"/> class with the shipped compiler and targets.
    /// </summary>
    public ScreenplayPlanning()
        : this(new SemanticModelCompiler(), new RenderTargetRoster())
    {
    }

    /// <inheritdoc/>
    public async Task<ScreenplayRenderPlan> Plan(ScreenplayRenderRequest request, CancellationToken cancellationToken)
    {
        var files = Files(request.SourcePath);
        if (files.Count == 0)
        {
            return new(0, [], null);
        }

        if (!targets.TryGet(request.Target, out var target))
        {
            return new(files.Count, [Error("CLI-RENDER-001", $"Renderer target '{request.Target}' is not bundled with this CLI.", null)], null);
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(request.ApplicationName));
        var root = File.Exists(request.SourcePath) ? Path.GetDirectoryName(request.SourcePath)! : request.SourcePath;
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var source = await File.ReadAllTextAsync(file, cancellationToken);
            documents.Add(SemanticSourceDocument.Create(
                catalog.ResolveDocument(relativePath),
                relativePath,
                relativePath,
                source));
        }

        var compilation = compiler.Compile(
            request.ApplicationName,
            SemanticDocumentSet.Create([.. documents], catalog));
        var diagnostics = compilation.Diagnostics.Select(Map).ToList();
        if (!compilation.Success)
        {
            return new(files.Count, diagnostics, null);
        }

        var execution = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        diagnostics.AddRange(execution.Issues.Select(issue =>
            Error($"PLAN-{issue.Kind.ToString().ToUpperInvariant()}", issue.Details, issue.Artifact.ToString())));
        if (!execution.Success)
        {
            return new(files.Count, diagnostics, null);
        }

        var artifacts = target!.Plan(compilation.Value.Model, execution.Plan!);
        diagnostics.AddRange(artifacts.Diagnostics.Select(Map));
        return new(files.Count, diagnostics, artifacts);
    }

    static IReadOnlyList<string> Files(string path) => File.Exists(path)
        ? [Path.GetFullPath(path)]
        : [.. Directory.EnumerateFiles(path, $"*{PlayFileTargetResolver.Extension}", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Order(StringComparer.Ordinal)];

    static ScreenplayDiagnostic Map(Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            $"{diagnostic.Location.Path ?? "Screenplay"}({diagnostic.Location.Line},{diagnostic.Location.Column})");

    static ScreenplayDiagnostic Map(ArtifactRenderDiagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Artifact.IsSet ? diagnostic.Artifact.ToString() : null);

    static ScreenplayDiagnostic Error(string code, string message, string? location) =>
        new(ScreenplayDiagnosticSeverity.Error, code, message, location);
}
