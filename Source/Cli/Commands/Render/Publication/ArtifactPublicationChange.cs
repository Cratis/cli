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
    public static ArtifactPublicationChange From(ArtifactOperation operation, string? beforeSha256, string? afterSha256) =>
        new(
            operation.Path,
            operation.Kind switch
            {
                ArtifactOperationKind.Write => "write",
                ArtifactOperationKind.Delete => "delete",
                _ => throw new UnsafeArtifactPublication("The publication operation kind is unsupported.")
            },
            beforeSha256,
            afterSha256);
}
