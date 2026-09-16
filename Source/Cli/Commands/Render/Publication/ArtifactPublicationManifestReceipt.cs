// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Describes the exact raw manifest bytes observed before and written by publication.
/// </summary>
/// <param name="BaseSha256">The raw prior manifest hash, or absent when there was no manifest.</param>
/// <param name="Sha256">The hash of the exact UTF-8 bytes passed to the manifest write.</param>
internal sealed record ArtifactPublicationManifestReceipt(string? BaseSha256, string Sha256)
{
    /// <summary>
    /// Gets the destination-relative ownership manifest path.
    /// </summary>
    public string Path => ArtifactPublicationStorage.ManifestFileName;
}
