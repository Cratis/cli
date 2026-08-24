// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Publishes complete artifact plans through staging, backups, a durable journal, and a manifest-last commit.
/// </summary>
/// <param name="observer">The publication checkpoint observer.</param>
internal sealed class ArtifactPublisher(IArtifactPublicationObserver observer) : IArtifactPublication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactPublisher"/> class.
    /// </summary>
    public ArtifactPublisher()
        : this(new ArtifactPublicationObserver())
    {
    }

    /// <inheritdoc/>
    public Task<bool> Recover(string destination, CancellationToken cancellationToken) =>
        Task.FromResult(ArtifactPublicationRecovery.Recover(destination, cancellationToken));

    /// <inheritdoc/>
    public async Task<ArtifactPublicationResult> Publish(
        ArtifactPublicationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var destination = Path.GetFullPath(request.Destination);
        var destinationExisted = Directory.Exists(destination);
        await Recover(destination, cancellationToken);
        var journalWritten = false;
        try
        {
            var prepared = ArtifactPublicationPreparation.Prepare(request with { Destination = destination });
            Stage(destination, prepared, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var journal = new ArtifactPublicationJournal(
                ArtifactPublicationJournal.CurrentSchemaVersion,
                false,
                false,
                prepared.PreviousManifestJson,
                prepared.Manifest,
                prepared.Operations);
            WriteJournal(destination, journal);
            journalWritten = true;
            observer.OnCheckpoint(ArtifactPublicationCheckpoint.JournalWritten);

            Backup(destination, prepared.Operations, cancellationToken);
            journal = journal with { BackupsComplete = true };
            WriteJournal(destination, journal);
            observer.OnCheckpoint(ArtifactPublicationCheckpoint.BackupsCompleted);

            Apply(destination, prepared, observer, cancellationToken);
            ArtifactPublicationStorage.WriteDurable(
                ArtifactPublicationStorage.ManifestPath(destination),
                ArtifactPublicationStorage.Serialize(prepared.Manifest));
            journal = journal with { ManifestPublished = true };
            WriteJournal(destination, journal);
            observer.OnCheckpoint(ArtifactPublicationCheckpoint.ManifestPublished);
            ArtifactPublicationStorage.Cleanup(destination);

            return new(
                prepared.Operations.Count(_ => _.Kind == ArtifactOperationKind.Write),
                prepared.Operations.Count(_ => _.Kind == ArtifactOperationKind.Delete),
                prepared.Unchanged);
        }
        catch
        {
            if (!journalWritten)
            {
                ArtifactPublicationStorage.Cleanup(destination);
                RemoveEmptyNewDestination(destination, destinationExisted);
            }

            throw;
        }
    }

    static void Stage(
        string destination,
        PreparedArtifactPublication prepared,
        CancellationToken cancellationToken)
    {
        foreach (var operation in prepared.Operations.Where(_ => _.Kind == ArtifactOperationKind.Write))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArtifactPublicationStorage.WriteDurable(
                ArtifactPublicationStorage.StagingPath(destination, operation.Path),
                prepared.PlannedArtifacts[operation.Path].Bytes.AsSpan());
        }
    }

    static void Backup(
        string destination,
        IReadOnlyList<ArtifactOperation> operations,
        CancellationToken cancellationToken)
    {
        foreach (var operation in operations.Where(_ => _.HadPrevious))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArtifactPublicationStorage.CopyAtomic(
                ArtifactPublicationStorage.ArtifactPath(destination, operation.Path),
                ArtifactPublicationStorage.BackupPath(destination, operation.Path));
        }
    }

    static void Apply(
        string destination,
        PreparedArtifactPublication prepared,
        IArtifactPublicationObserver observer,
        CancellationToken cancellationToken)
    {
        foreach (var operation in prepared.Operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = ArtifactPublicationStorage.ArtifactPath(destination, operation.Path);
            if (operation.Kind == ArtifactOperationKind.Write)
            {
                ArtifactPublicationStorage.CopyAtomic(
                    ArtifactPublicationStorage.StagingPath(destination, operation.Path),
                    path);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }

            observer.OnCheckpoint(ArtifactPublicationCheckpoint.OperationApplied);
        }
    }

    static void WriteJournal(string destination, ArtifactPublicationJournal journal) =>
        ArtifactPublicationStorage.WriteDurable(
            ArtifactPublicationStorage.JournalPath(destination),
            ArtifactPublicationStorage.Serialize(journal));

    static void RemoveEmptyNewDestination(string destination, bool destinationExisted)
    {
        if (!destinationExisted && Directory.Exists(destination) && !Directory.EnumerateFileSystemEntries(destination).Any())
        {
            Directory.Delete(destination);
        }
    }
}
