// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Shares the bundled target's names, in-memory admission and source diagnostic mapping.
/// </summary>
internal static class CratisRenderAdmission
{
    internal static CratisRenderingOptions RenderingOptions(string applicationName, string? projectName = null, string? rootNamespace = null, ImmutableArray<string> fileHeader = default) =>
        new(projectName ?? applicationName, rootNamespace ?? applicationName) { FileHeader = fileHeader.IsDefault ? [] : fileHeader };

    internal static CratisPlanOptions Options(string applicationName, string? projectName = null, string? rootNamespace = null, bool strict = false)
    {
        var rendering = RenderingOptions(applicationName, projectName, rootNamespace);
        return new(applicationName, rendering.ProjectName, rendering.RootNamespace) { Strict = strict };
    }

    internal static (ArtifactRenderPlan? Plan, IReadOnlyList<ScreenplayDiagnostic> Diagnostics) Check(
        CompilationResult<SemanticCompilation> bound,
        ImmutableArray<Diagnostic> attachmentDiagnostics,
        CancellationToken cancellationToken)
    {
        var compilation = bound.Value!;
        var request = new ScreenplayDocumentRenderRequest(compilation.Documents, compilation.Model.Application.Name, CratisRendering.TargetId)
        {
            AttachmentDiagnostics = attachmentDiagnostics
        };
        var planned = new ScreenplayPlanning().PlanCompiled(request, bound, null, null, cancellationToken);
        return (planned.Artifacts, planned.Diagnostics);
    }

    internal static ScreenplayDiagnostic Map(ArtifactRenderDiagnostic diagnostic, SemanticCompilation compilation) =>
        Map(diagnostic.Severity, diagnostic.Code, diagnostic.Message, diagnostic.Artifact.IsSet ? diagnostic.Artifact.ToString() : null, compilation);

    static ScreenplayDiagnostic Map(ArtifactRenderDiagnosticSeverity severity, string code, string message, string? location, SemanticCompilation compilation)
    {
        var subject = location?.StartsWith("sem1:", StringComparison.Ordinal) == true ? location : null;
        var entry = subject is null ? null : compilation.SourceMap.Entries
            .Where(entry => entry.SemanticId.ToString() == subject)
            .OrderBy(entry => entry.Role != SemanticSourceMapRole.Declaration)
            .FirstOrDefault();
        if (entry is not null)
        {
            var document = compilation.Documents.Documents.First(document => document.Id == entry.Span.Document);
            location = $"{document.DisplayPath}({entry.Span.StartLine},{entry.Span.StartColumn})";
        }

        return new((ScreenplayDiagnosticSeverity)(int)severity, code, message, location) { Subject = subject };
    }
}
