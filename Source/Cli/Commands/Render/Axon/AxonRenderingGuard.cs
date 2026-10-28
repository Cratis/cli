// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Axon;

namespace Cratis.Cli.Commands.Render.Axon;

/// <summary>
/// Converts unexpected renderer failures into a non-publishable diagnostic plan.
/// </summary>
internal static class AxonRenderingGuard
{
    internal const string RendererFailureCode = "CLI-RENDER-031";

    internal static ArtifactRenderPlan Plan(IAxonRendering rendering, AxonRenderInput input)
    {
        try
        {
            return rendering.Plan(input);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var request = new ArtifactRenderRequest(input.Model, input.ExecutionPlan, AxonRendering.CreateProfile(strict: input.Strict), input.Scope)
            {
                AdditionalScopes = input.AdditionalScopes
            };
            var diagnostic = new ArtifactRenderDiagnostic(
                RendererFailureCode,
                ArtifactRenderDiagnosticSeverity.Error,
                $"Target axon renderer Cratis.Stage.Rendering.Axon {AxonRendering.Version} failed: {exception.GetType().Name}: {exception.Message}; this is a renderer bug; please report it at https://github.com/Cratis/Stage.Axon/issues",
                input.Model.Application.Id);
            return ArtifactRenderPlan.Create(request, [], [diagnostic], [], [], []);
        }
    }
}
