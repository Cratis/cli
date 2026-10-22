// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Shares the bundled target's names, in-memory admission and source diagnostic mapping.
/// </summary>
internal static class CratisRenderAdmission
{
    internal static CratisRenderingOptions RenderingOptions(string applicationName, string? projectName = null, string? rootNamespace = null) =>
        new(projectName ?? applicationName, rootNamespace ?? applicationName);

    internal static CratisPlanOptions Options(string applicationName, string? projectName = null, string? rootNamespace = null)
    {
        var rendering = RenderingOptions(applicationName, projectName, rootNamespace);
        return new(applicationName, rendering.ProjectName, rendering.RootNamespace);
    }

    internal static (CratisPlanResult? Plan, IReadOnlyList<ScreenplayDiagnostic> Diagnostics) Check(
        CompilationResult<SemanticCompilation> bound,
        ImmutableArray<Diagnostic> attachmentDiagnostics)
    {
        var compilation = bound.Value!;
        if (RenderedSemanticVersions.Check(compilation.Model.SemanticVersion) is { } notAdmitted)
        {
            return (null, [notAdmitted]);
        }

        var execution = SemanticExecutionPlan.Compile(compilation.Model);
        if (!execution.Success)
        {
            return (null, [.. execution.Issues.Select(issue => Map(
                ArtifactRenderDiagnosticSeverity.Error,
                $"PLAN-{issue.Kind.ToString().ToUpperInvariant()}",
                issue.Details,
                issue.Artifact.ToString(),
                compilation))]);
        }

        try
        {
            var loaded = new LoadedSemanticModel(compilation.Model, execution.Plan!)
            {
                SpecificationOrigins = compilation.SpecificationOrigins,
                ImplementationRequirements = bound.ImplementationRequirements,
                ImplementationContents = RenderImplementationBodies.Resolve(compilation.Documents, bound.ImplementationRequirements),
                TypedContextDescriptors = bound.TypedContextDescriptors,
                AttachmentDiagnostics = attachmentDiagnostics
            };
            var selection = new PlanSelection([.. compilation.Model.Application.Modules.Select(module => PlanSelectionEntry.Module(module.Name))]);
            var plan = CratisRendering.PlanFrom(loaded, selection, Options(compilation.Model.Application.Name));
            return (plan, [.. plan.Diagnostics.Select(diagnostic => Map(diagnostic, compilation))]);
        }
        catch (InvalidCratisBackendApplicationScaffold exception)
        {
            return (null, [new(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-002", exception.Message, null)]);
        }
        catch (InvalidSemanticContract exception)
        {
            return (null, [new(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-005", $"The bundled renderer violated its contract. Nothing was planned or published. Report this to Cratis/Stage with this message: {exception.Message}", null)]);
        }
        catch (InvalidArtifactRenderContract exception)
        {
            return (null, [new(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-005", exception.Message, null)]);
        }
    }

    internal static ScreenplayDiagnostic Map(ArtifactRenderDiagnostic diagnostic, SemanticCompilation compilation) =>
        Map(diagnostic.Severity, diagnostic.Code, diagnostic.Message, diagnostic.Artifact.IsSet ? diagnostic.Artifact.ToString() : null, compilation);

    static ScreenplayDiagnostic Map(CratisPlanDiagnostic diagnostic, SemanticCompilation compilation) =>
        Map(diagnostic.Severity, diagnostic.Code, diagnostic.Message, diagnostic.Source ?? (diagnostic.Artifact.IsSet ? diagnostic.Artifact.ToString() : null), compilation) with
        {
            Subject = diagnostic.Artifact.IsSet ? diagnostic.Artifact.ToString() : diagnostic.Source?.StartsWith("sem1:", StringComparison.Ordinal) == true ? diagnostic.Source : null
        };

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
