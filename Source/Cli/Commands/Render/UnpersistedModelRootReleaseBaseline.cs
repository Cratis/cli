// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Warns about a root baseline that cannot be checked without persisted identities.
/// </summary>
/// <param name="root">The source root without persisted identities.</param>
internal sealed class UnpersistedModelRootReleaseBaseline(string root) : IRenderReleaseBaseline
{
    /// <inheritdoc/>
    public ReleaseBaselineAssessment Assess(SemanticCompilation compilation)
    {
        var path = Path.Combine(root, ".screenplay", ReleaseBaselineStore.FileName);

        return File.Exists(path) || Directory.Exists(path)
            ? new(ReleaseBaselineAssessmentStatus.Unavailable, compilation.Model.Revision, [], "PATH renders need persisted .screenplay/identities.json; run cratis screenplay baseline check")
            : ReleaseBaselineAssessment.NotSupplied;
    }
}
