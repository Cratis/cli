// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents the statically bundled Cratis ESM renderer target, delegating to the published
/// <see cref="CratisRendering"/> facade for the exact target profile and scaffold.
/// </summary>
internal sealed class CratisRenderTarget : IRenderTarget
{
    /// <inheritdoc/>
    public string Name => CratisRendering.TargetId;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(ExecutableSemanticModel model, SemanticExecutionPlan executionPlan)
    {
        var options = new CratisRenderingOptions(model.Application.Name, model.Application.Name);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);
        return CratisRendering.Plan(model, executionPlan, scope, options);
    }
}
