// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Axon;

/// <summary>
/// Refuses publication until the reviewed Axon facade is available in a released Stage package.
/// </summary>
internal sealed class UnavailableAxonRendering : IAxonRendering
{
    /// <inheritdoc/>
    public string Target => "axon";

    /// <inheritdoc/>
    public SemanticVersion NewestAdmittedSemanticVersion => SemanticVersion.V34;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(AxonRenderInput input)
    {
        var profile = ArtifactRenderProfile.Create(Target, "0.0.0", "unavailable", "0.0.0", []);
        var request = new ArtifactRenderRequest(input.Model, input.ExecutionPlan, profile, input.Scope)
        {
            AdditionalScopes = input.AdditionalScopes
        };
        return ArtifactRenderPlan.Create(
            request,
            [],
            [new("CLI-RENDER-028", ArtifactRenderDiagnosticSeverity.Error, "The axon render target needs the Cratis.Stage.Rendering.Axon package, which this CLI build does not bundle yet.", input.Model.Application.Id)],
            [],
            [],
            []);
    }
}
