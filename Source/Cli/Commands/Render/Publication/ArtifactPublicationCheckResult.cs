// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Describes a read-only publication decision against the real destination.
/// </summary>
/// <param name="Written">The number of artifacts that would be written.</param>
/// <param name="Removed">The number of artifacts that would be removed.</param>
/// <param name="Unchanged">The number of artifacts already matching the plan.</param>
/// <param name="Refused">The number of refusals.</param>
/// <param name="RecoveryPending">Whether interrupted control state requires recovery.</param>
/// <param name="Receipt">The read-only decision receipt.</param>
internal sealed record ArtifactPublicationCheckResult(
    int Written,
    int Removed,
    int Unchanged,
    int Refused,
    bool RecoveryPending,
    ArtifactPublicationCheckReceipt Receipt);

/// <summary>
/// Describes proposed artifact outcomes without claiming any publication took place.
/// </summary>
/// <param name="Changes">One verdict per artifact path, plus destination-level refusals with an absent path.</param>
/// <param name="Manifest">The prior and proposed manifest hashes, when planning permits them.</param>
internal sealed record ArtifactPublicationCheckReceipt(
    IReadOnlyList<ArtifactPublicationVerdict> Changes,
    ArtifactPublicationManifestReceipt? Manifest)
{
    /// <summary>
    /// Gets the receipt wire schema version.
    /// </summary>
    public string SchemaVersion => "2";

    /// <summary>
    /// Gets the checked paths grouped by semantic declaration.
    /// </summary>
    public IReadOnlyList<ArtifactSourcePaths> BySource => ArtifactSourcePaths.Group(Changes.Where(change => change.Path is not null).Select(change => (change.Path!, change.Sources)));

    /// <summary>
    /// Gets the read-only decision status.
    /// </summary>
    public string Status => "check";
}
