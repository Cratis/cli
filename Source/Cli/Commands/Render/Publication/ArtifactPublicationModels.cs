// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Publication;

internal enum ArtifactOperationKind
{
    Write = 0,
    Delete = 1
}

internal enum ArtifactPublicationCheckpoint
{
    JournalWritten = 0,
    BackupsCompleted = 1,
    OperationApplied = 2,
    ManifestPublished = 3
}

internal interface IArtifactPublicationObserver
{
    void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint);
}

internal sealed class ArtifactPublicationObserver : IArtifactPublicationObserver
{
    public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint) => _ = checkpoint;
}

internal sealed record ManagedArtifact(string Path, string Sha256);

internal sealed record ArtifactManifest(
    string SchemaVersion,
    string ArtifactPlanSchemaVersion,
    string SemanticRevision,
    string Target,
    string TargetVersion,
    string Renderer,
    string RendererVersion,
    string ApplicationName,
    IReadOnlyList<ManagedArtifact> Artifacts)
{
    public const string CurrentSchemaVersion = "1";

    public static ArtifactManifest From(ArtifactRenderPlan plan) =>
        new(
            CurrentSchemaVersion,
            plan.SchemaVersion,
            plan.SemanticRevision.ToString(),
            plan.Target,
            plan.TargetVersion,
            plan.Renderer,
            plan.RendererVersion,
            plan.ApplicationName,
            [.. plan.Artifacts.Select(_ => new ManagedArtifact(_.RelativePath, _.Sha256))]);
}

internal sealed record ArtifactOperation(ArtifactOperationKind Kind, string Path, bool HadPrevious);

internal sealed record ArtifactPublicationJournal(
    string SchemaVersion,
    bool BackupsComplete,
    bool ManifestPublished,
    string? PreviousManifestJson,
    ArtifactManifest NextManifest,
    IReadOnlyList<ArtifactOperation> Operations)
{
    public const string CurrentSchemaVersion = "1";
}
