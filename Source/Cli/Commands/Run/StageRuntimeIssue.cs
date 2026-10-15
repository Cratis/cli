// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// A capability the Stage cannot execute for a modeled artifact.
/// </summary>
/// <param name="Capability">The unsupported capability.</param>
/// <param name="Artifact">The modeled artifact.</param>
/// <param name="Details">The reason the artifact cannot execute.</param>
public sealed record StageRuntimeIssue(string Capability, string Artifact, string Details)
{
    /// <summary>
    /// Gets a value indicating whether Stage directs this capability to its semantic engine.
    /// </summary>
    public bool PointsToSemantic => Details.Contains("semantic path", StringComparison.OrdinalIgnoreCase) ||
        Details.Contains("semantic engine", StringComparison.OrdinalIgnoreCase);
}
