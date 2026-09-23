// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public ArtifactRenderPlan Plan(ExecutableSemanticModel model, SemanticExecutionPlan executionPlan, string? projectName = null, string? rootNamespace = null)
    {
        var options = new CratisRenderingOptions(projectName ?? model.Application.Name, rootNamespace ?? model.Application.Name);
        var profile = CratisRendering.CreateProfile(model.Application.Name, options);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);
        return planner.Plan(new(model, executionPlan, profile, scope));
    }
}
