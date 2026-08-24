// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;

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
    /// Gets a value indicating whether the plan is complete and publishable.
    /// </summary>
    public bool Success => Documents > 0 && Artifacts?.Success == true &&
        Diagnostics.All(_ => _.Severity != ScreenplayDiagnosticSeverity.Error);
}
