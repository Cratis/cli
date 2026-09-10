// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Defines crash recovery and safe artifact publication.
/// </summary>
internal interface IArtifactPublication
{
    /// <summary>
    /// Recovers an interrupted prior publication at a destination.
    /// </summary>
    /// <param name="destination">The resolved destination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when interrupted state was recovered or completed.</returns>
    Task<bool> Recover(string destination, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a complete plan through staging and a durable journal.
    /// </summary>
    /// <param name="request">The publication request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The publication result.</returns>
    Task<ArtifactPublicationResult> Publish(ArtifactPublicationRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// The exception that is thrown when publication cannot preserve managed or user-owned files safely.
/// </summary>
/// <param name="message">The unsafe condition.</param>
internal sealed class UnsafeArtifactPublication(string message) : Exception(message);

/// <summary>
/// Represents one safe artifact publication request.
/// </summary>
/// <param name="Plan">The complete publishable artifact plan.</param>
/// <param name="Destination">The resolved destination directory.</param>
/// <param name="Force">Whether modified previously managed active files may be replaced.</param>
internal sealed record ArtifactPublicationRequest(ArtifactRenderPlan Plan, string Destination, bool Force);

/// <summary>
/// Represents the result of one artifact publication.
/// </summary>
/// <param name="Written">The number of created or replaced artifacts.</param>
/// <param name="Removed">The number of unchanged stale managed artifacts removed.</param>
/// <param name="Unchanged">The number of planned artifacts already matching their hash.</param>
internal sealed record ArtifactPublicationResult(int Written, int Removed, int Unchanged);
