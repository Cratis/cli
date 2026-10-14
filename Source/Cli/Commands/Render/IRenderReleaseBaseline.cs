// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Assesses release metadata against the whole application before target planning.
/// </summary>
internal interface IRenderReleaseBaseline
{
    /// <summary>
    /// Checks admitted event revisions without writing model metadata.
    /// </summary>
    /// <param name="compilation">The whole application being rendered.</param>
    /// <returns>The nonblocking release-baseline assessment.</returns>
    ReleaseBaselineAssessment Assess(SemanticCompilation compilation);
}
