// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

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

        if (!targets.TryGet(request.Target, out _))
        {
            return new(files.Count, [Error("CLI-RENDER-001", $"Renderer target '{request.Target}' is not bundled with this CLI.", null)], null);
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(request.ApplicationName));
        var root = File.Exists(request.SourcePath) ? Path.GetDirectoryName(Path.GetFullPath(request.SourcePath))! : request.SourcePath;
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var source = await File.ReadAllTextAsync(file, cancellationToken);

            // A legacy file has no persisted document identity. Bootstrap an opaque key from its
            // portable relative path; never pass directory separators to the non-path key contract.
            var key = $"file-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(relativePath.Normalize(NormalizationForm.FormC))))}";
            documents.Add(SemanticSourceDocument.Create(
                catalog.ResolveDocument(key),
                key,
                relativePath,
                source));
        }

        var attachments = AttachmentFiles.Load(root, [.. documents]);
        var documentRequest = new ScreenplayDocumentRenderRequest(
            SemanticDocumentSet.Create([.. documents], catalog, attachments.Contents),
            request.ApplicationName,
            request.Target,
            request.ProjectName,
            request.RootNamespace)
        {
            AttachmentDiagnostics = attachments.Diagnostics
        };
        return PlanDocuments(documentRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public ScreenplayRenderPlan PlanDocuments(ScreenplayDocumentRenderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = request.Documents.Documents.Length;
        if (!targets.TryGet(request.Target, out var target))
        {
            return new(count, [Error("CLI-RENDER-001", $"Renderer target '{request.Target}' is not bundled with this CLI.", null)], null);
        }

        var compilation = compiler.Compile(request.ApplicationName, request.Documents);
        var diagnostics = compilation.Diagnostics.Select(Map).ToList();
        diagnostics.AddRange(request.AttachmentDiagnostics.Select(Map));
        if (!compilation.Success)
        {
            return new(count, diagnostics, null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var execution = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        diagnostics.AddRange(execution.Issues.Select(issue =>
            Error($"PLAN-{issue.Kind.ToString().ToUpperInvariant()}", issue.Details, issue.Artifact.ToString())));
        if (!execution.Success)
        {
            return new(count, diagnostics, null);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contents = RenderImplementationBodies.Resolve(request.Documents, compilation.ImplementationRequirements);
            var artifacts = target!.Plan(
                compilation.Value.Model,
                execution.Plan!,
                request.ProjectName,
                request.RootNamespace,
                compilation.ImplementationRequirements,
                contents,
                request.AttachmentDiagnostics);
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.AddRange(artifacts.Diagnostics.Select(Map));
            if (compilation.Value.Model.SemanticVersion == SemanticVersion.V4 &&
                artifacts.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-016"))
            {
                var evolvedEvent = compilation.Value.Model.Application.Modules
                    .SelectMany(module => module.Features)
                    .SelectMany(AllFeatures)
                    .SelectMany(feature => feature.Slices)
                    .SelectMany(slice => slice.Events)
                    .FirstOrDefault(@event => !@event.PriorRevisions.IsDefaultOrEmpty);
                if (evolvedEvent is not null)
                {
                    diagnostics.Add(Error("CLI-RENDER-003", $"Event '{evolvedEvent.Name}' has multiple generations (ESM v4); this Stage renderer admits only up to ESM v3 and cannot render it.", null));
                }
            }
            return new(count, diagnostics, artifacts);
        }
        catch (InvalidCratisBackendApplicationScaffold exception)
        {
            diagnostics.Add(Error("CLI-RENDER-002", exception.Message, null));
            return new(count, diagnostics, null);
        }
    }

    static IEnumerable<SemanticFeature> AllFeatures(SemanticFeature feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

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
