// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Describes one prepared artifact operation; an absent hash means the path was absent at that boundary.
/// </summary>
/// <param name="Path">The destination-relative artifact path.</param>
/// <param name="Kind">The stable wire kind: write or delete.</param>
/// <param name="BeforeSha256">The actual hash observed during ownership checking, or absent for creation.</param>
/// <param name="AfterSha256">The planned hash, or absent for deletion.</param>
internal sealed record ArtifactPublicationChange(string Path, string Kind, string? BeforeSha256, string? AfterSha256)
{
    /// <summary>
    /// Gets the declarations this artifact realizes, in artifact source order.
    /// </summary>
    public IReadOnlyList<ArtifactSource> Sources { get; init; } = [];
}
