// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Checks a workspace baseline against the exact compilation supplied to Stage.
/// </summary>
/// <param name="read">The bounded, read-only baseline input.</param>
internal sealed class FileReleaseBaseline(ReleaseBaselineRead read) : IRenderReleaseBaseline
{
    /// <inheritdoc/>
    public ReleaseBaselineAssessment Assess(SemanticCompilation compilation) => StageReleaseBaseline.Check(compilation, read);
}
