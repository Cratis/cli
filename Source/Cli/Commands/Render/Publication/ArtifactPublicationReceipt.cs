// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Describes the observed publication after manifest publication and control-state cleanup succeed.
/// </summary>
/// <param name="Changes">Artifact changes in prepared operation order.</param>
/// <param name="Manifest">The separate ownership manifest boundary.</param>
internal sealed record ArtifactPublicationReceipt(
    IReadOnlyList<ArtifactPublicationChange> Changes,
    ArtifactPublicationManifestReceipt Manifest)
{
    /// <summary>
    /// Gets the receipt wire schema version.
    /// </summary>
    public string SchemaVersion => "1";

    /// <summary>
    /// Gets the successful filesystem publication status, not a Git commit status.
    /// </summary>
    public string Status => "published";
}
