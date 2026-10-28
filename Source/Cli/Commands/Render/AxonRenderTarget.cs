// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Render.Axon;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Maps CLI compiler inputs into the experimental Axon renderer without Cratis-specific preparation.
/// </summary>
/// <param name="rendering">The package boundary, or its unavailable implementation.</param>
internal sealed class AxonRenderTarget(IAxonRendering rendering) : IRenderTarget
{
    /// <summary>Initializes a new instance of the <see cref="AxonRenderTarget"/> class.</summary>
    public AxonRenderTarget() : this(new PackagedAxonRendering())
    {
    }

    /// <inheritdoc/>
    public string Name => rendering.Target;

    /// <inheritdoc/>
    public SemanticVersion NewestAdmittedSemanticVersion => rendering.NewestAdmittedSemanticVersion;

    /// <inheritdoc/>
    public bool UsesCratisConventions => false;

    /// <inheritdoc/>
    public bool SupportsStrictAcceptance => false;

    /// <inheritdoc/>
    public ScreenplayDiagnostic? CheckSemanticVersion(SemanticVersion version) => NewestAdmittedSemanticVersion.IsAtLeast(version)
        ? null
        : new(
            ScreenplayDiagnosticSeverity.Error,
            RenderedSemanticVersions.NotAdmittedCode,
            $"The model compiles to ESM v{version}, but 'cratis render --target axon' admits versions through v{NewestAdmittedSemanticVersion}. Newer model versions require explicit renderer admission, so nothing was planned or published.",
            null);

    /// <inheritdoc/>
    public CratisPlanResult? PlanScaffold(CratisPlanOptions options) => null;

    /// <inheritdoc/>
    public RenderPublicationRefusal? CheckPublication(ArtifactRenderPlan plan, Func<string, string?> readExisting) => null;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(
        SemanticCompilation compilation,
        SemanticExecutionPlan executionPlan,
        string? projectName,
        string? rootNamespace,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<SemanticTypedContextDescriptor> typedContextDescriptors,
        ImmutableArray<Diagnostic> attachmentDiagnostics,
        ICollection<ScreenplayDiagnostic> warnings,
        SceneApplication? scene = null,
        RenderSelection? selection = null,
        ApprovedPackageSet? approvedPackages = null,
        ReleaseBaselineAssessment? releaseBaseline = null,
        ArtifactRenderInput? renderingInput = null,
        IReadOnlyList<ReducerMigrationEvidence>? migrations = null,
        ImmutableArray<string> fileHeader = default,
        RenderStringsCatalog? strings = null,
        bool strict = false)
    {
        WarnAboutStrictAcceptance(strict, warnings);
        if (renderingInput is not null)
        {
            warnings.Add(new(ScreenplayDiagnosticSeverity.Warning, "CLI-RENDER-030", "The Axon target ignores Cratis reducer rendering input.", null));
        }
        if (migrations is { Count: > 0 })
        {
            warnings.Add(new(ScreenplayDiagnosticSeverity.Warning, "CLI-RENDER-030", "The Axon target ignores Cratis reducer migration evidence.", null));
        }
        if (approvedPackages is { IsEmpty: false })
        {
            warnings.Add(new(ScreenplayDiagnosticSeverity.Warning, "CLI-RENDER-030", "The Axon target ignores --package-set.", null));
        }
        return rendering.Plan(new(
            compilation.Model,
            executionPlan,
            selection?.Scopes[0] ?? new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id))
        {
            ProjectName = projectName,
            JavaPackage = rootNamespace,
            AdditionalScopes = selection?.Scopes.RemoveAt(0) ?? [],
            ImplementationRequirements = requirements,
            ImplementationContents = contents,
            TypedContextDescriptors = typedContextDescriptors,
            AttachmentDiagnostics = attachmentDiagnostics,
            ReleaseBaseline = releaseBaseline ?? ReleaseBaselineAssessment.NotSupplied,
            FileHeader = fileHeader.IsDefault ? [] : fileHeader,
            Strict = false
        });
    }

    static void WarnAboutStrictAcceptance(bool strict, ICollection<ScreenplayDiagnostic> warnings)
    {
        if (strict)
        {
            warnings.Add(new(ScreenplayDiagnosticSeverity.Warning, "CLI-RENDER-030", "The Axon target ignores --strict because it has no Stage strict acceptance contract.", null));
        }
    }
}
