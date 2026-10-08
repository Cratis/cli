// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents the statically bundled Cratis ESM renderer target, using the published
/// <see cref="CratisRendering"/> profile policy and package-owned artifact planner.
/// </summary>
/// <param name="planner">The artifact planner consuming the complete package-owned profile.</param>
internal sealed class CratisRenderTarget(IArtifactRenderPlanner planner) : IRenderTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CratisRenderTarget"/> class with the shipped planner.
    /// </summary>
    public CratisRenderTarget()
        : this(new CratisArtifactRenderPlanner())
    {
    }

    /// <inheritdoc/>
    public string Name => CratisRendering.TargetId;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(
        SemanticCompilation compilation,
        SemanticExecutionPlan executionPlan,
        string? projectName,
        string? rootNamespace,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<SemanticTypedContextDescriptor> typedContextDescriptors,
        ImmutableArray<Diagnostic> attachmentDiagnostics)
    {
        var model = compilation.Model;
        var options = new CratisRenderingOptions(projectName ?? model.Application.Name, rootNamespace ?? model.Application.Name);
        var profile = CratisRendering.WithAuthoringMetadata(CratisRendering.CreateProfile(model.Application.Name, options), compilation);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);
        return planner.Plan(new ArtifactRenderRequest(model, executionPlan, profile, scope)
        {
            ImplementationRequirements = requirements,
            ImplementationContents = contents,
            TypedContextDescriptors = typedContextDescriptors,
            AttachmentDiagnostics = attachmentDiagnostics
        });
    }
}
