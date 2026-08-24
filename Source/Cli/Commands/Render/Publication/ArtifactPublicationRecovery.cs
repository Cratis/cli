// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

internal static class ArtifactPublicationRecovery
{
    public static bool Recover(string destination, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        destination = Path.GetFullPath(destination);
        if (!Directory.Exists(destination))
        {
            return false;
        }

        var journal = ArtifactPublicationStorage.ReadJournal(destination);
        if (journal is null)
        {
            return CleanupAbandonedStaging(destination);
        }

        Validate(journal);
        if (journal.ManifestPublished || !journal.BackupsComplete)
        {
            ArtifactPublicationStorage.Cleanup(destination);
            return true;
        }

        foreach (var operation in journal.Operations.Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArtifactPublicationStorage.EnsureSafePath(destination, operation.Path);
            RestoreOperation(destination, operation);
        }

        RestoreManifest(destination, journal.PreviousManifestJson);
        ArtifactPublicationStorage.Cleanup(destination);
        return true;
    }

    static bool CleanupAbandonedStaging(string destination)
    {
        if (!Directory.Exists(ArtifactPublicationStorage.ControlPath(destination)))
        {
            return false;
        }

        if (!File.Exists(ArtifactPublicationStorage.ManifestPath(destination)))
        {
            throw new UnsafeArtifactPublication("An unmanaged .cratis-render control directory already exists.");
        }

        ArtifactPublicationStorage.Cleanup(destination);
        return true;
    }

    static void RestoreOperation(string destination, ArtifactOperation operation)
    {
        var path = ArtifactPublicationStorage.ArtifactPath(destination, operation.Path);
        if (operation.HadPrevious)
        {
            var backup = ArtifactPublicationStorage.BackupPath(destination, operation.Path);
            if (!File.Exists(backup))
            {
                throw new UnsafeArtifactPublication($"Recovery backup for '{operation.Path}' is missing.");
            }

            ArtifactPublicationStorage.CopyAtomic(backup, path);
        }
        else if (operation.Kind == ArtifactOperationKind.Write && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    static void RestoreManifest(string destination, string? previousManifest)
    {
        var path = ArtifactPublicationStorage.ManifestPath(destination);
        if (previousManifest is null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        else
        {
            ArtifactPublicationStorage.WriteDurable(path, previousManifest);
        }
    }

    static void Validate(ArtifactPublicationJournal journal)
    {
        if (journal.SchemaVersion != ArtifactPublicationJournal.CurrentSchemaVersion ||
            journal.NextManifest.SchemaVersion != ArtifactManifest.CurrentSchemaVersion ||
            journal.Operations.Any(_ => !Enum.IsDefined(_.Kind) || string.IsNullOrWhiteSpace(_.Path)) ||
            journal.Operations.Select(_ => _.Path).Distinct(StringComparer.Ordinal).Count() != journal.Operations.Count)
        {
            throw new UnsafeArtifactPublication("The publication journal schema requires an explicit migration.");
        }
    }
}
