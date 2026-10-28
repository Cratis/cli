// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents one completely planned Screenplay rendering before publication.
/// </summary>
/// <param name="Documents">The number of source documents compiled.</param>
/// <param name="Diagnostics">Compiler, execution-plan, and renderer diagnostics.</param>
/// <param name="Artifacts">The immutable Stage artifact plan, when semantic binding reached target planning.</param>
internal sealed record ScreenplayRenderPlan(
    int Documents,
    IReadOnlyList<ScreenplayDiagnostic> Diagnostics,
    ArtifactRenderPlan? Artifacts)
{
    /// <summary>
    /// Gets the declaration addresses from the compiled model, without altering the artifact plan.
    /// </summary>
    public SemanticAddressIndex SemanticAddresses { get; init; } = SemanticAddressIndex.Empty;

    /// <summary>
    /// Gets the selected scopes, or null for application scope.
    /// </summary>
    public RenderSelection? Selection { get; init; }

    /// <summary>
    /// Gets application-wide scaffold names for a fresh scoped destination.
    /// </summary>
    public CratisPlanOptions? ScaffoldOptions { get; init; }

    /// <summary>Gets the target that owns this plan's publication policy.</summary>
    public IRenderTarget Target { get; init; } = new CratisRenderTarget();

    /// <summary>
    /// Gets a value indicating whether the plan is complete and publishable.
    /// </summary>
    public bool Success => Documents > 0 && Artifacts?.CanPublishArtifacts == true &&
        (PlanningDiagnostics ?? Diagnostics).All(_ => _.Severity != ScreenplayDiagnosticSeverity.Error);

    /// <summary>
    /// Gets diagnostics before Stage's artifact and acceptance diagnostics were added.
    /// Stage independently determines artifact safety through CanPublishArtifacts.
    /// </summary>
    public IReadOnlyList<ScreenplayDiagnostic>? PlanningDiagnostics { get; init; }
}
