// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ArtifactPublisher.given;

internal static class AcceptancePlans
{
    internal static ArtifactRenderPlan Degraded(ArtifactRenderRequest request, bool unsafeGeneration = false)
    {
        var rendered = new CratisArtifactRenderPlanner().Plan(request);
        var warning = new ArtifactRenderDiagnostic(ArtifactRenderDegradationCodes.UnsupportedConstruct, ArtifactRenderDiagnosticSeverity.Warning, "Package-backed degradation evidence", request.Model.Application.Id);
        var diagnostics = rendered.Diagnostics.Add(warning);
        if (unsafeGeneration)
        {
            // The same code must not cause an original error to be mistaken for the warning evidence.
            diagnostics = diagnostics.Add(warning with { Severity = ArtifactRenderDiagnosticSeverity.Error });
        }
        var plan = ArtifactRenderPlan.Create(request, rendered.Artifacts, diagnostics, rendered.ImplementationGaps, rendered.Refusals, rendered.RequiredRemovals);
        var input = request.Profile.Inputs.SingleOrDefault(input => input.Name == StrictAcceptanceInput.Name);
        var strict = input is not null && StrictAcceptanceInput.TryRead(input, out var enabled) && enabled;
        return plan.WithAcceptance([new(warning, "Supply the missing implementation.")], strict);
    }
}
