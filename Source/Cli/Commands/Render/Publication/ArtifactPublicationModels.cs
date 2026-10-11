// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
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

internal sealed record ManagedArtifact(string Path, string Sha256)
{
    public IReadOnlyList<string> Sources { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Owners { get; init; }
}

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
    public const string CurrentSchemaVersion = "2";

    public static ArtifactManifest From(ArtifactRenderPlan plan, SemanticAddressIndex? addresses = null, IEnumerable<PlannedArtifact>? artifacts = null) =>
        new(
            CurrentSchemaVersion,
            plan.SchemaVersion,
            plan.SemanticRevision.ToString(),
            plan.Target,
            plan.TargetVersion,
            plan.Renderer,
            plan.RendererVersion,
            plan.ApplicationName,
            [.. (artifacts ?? plan.Artifacts).Select(artifact => Managed(artifact, addresses))]);

    static ManagedArtifact Managed(PlannedArtifact artifact, SemanticAddressIndex? addresses)
    {
        var owners = artifact.Sources.SelectMany(id => addresses?.Owners(id.ToString()) ?? []).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        return new(artifact.RelativePath, artifact.Sha256)
        {
            Sources = [.. artifact.Sources.Select(id => id.ToString())],
            Owners = owners.Length == 0 ? null : owners
        };
    }
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
