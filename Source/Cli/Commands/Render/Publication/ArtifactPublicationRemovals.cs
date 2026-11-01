// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>
/// Turns a plan's required removals into hash-verified, journaled delete operations, or refusals.
/// </summary>
/// <remarks>
/// A required removal is an ordinary journaled delete that carries the Stage hash it was verified against:
/// the file is backed up before anything changes, the backup is verified against that hash, and recovery
/// verifies it again before restoring the file.
/// </remarks>
internal static class ArtifactPublicationRemovals
{
    /// <summary>
    /// The diagnostic code for a required removal the destination does not match.
    /// </summary>
    public const string RefusalCode = "CLI-RENDER-025";

    /// <summary>
    /// Adds the operations, verdicts and refusals for the plan's required removals.
    /// </summary>
    /// <param name="destination">The full destination path.</param>
    /// <param name="plan">The artifact plan.</param>
    /// <param name="planned">The planned artifacts by path.</param>
    /// <param name="operations">The operations to add delete operations to.</param>
    /// <param name="verdicts">The verdicts to add to.</param>
    /// <param name="refusals">The refusals to add to.</param>
    /// <returns>The paths of every required removal, which ordinary stale deletion must skip.</returns>
    public static IReadOnlySet<string> Analyze(
        string destination,
        ArtifactRenderPlan plan,
        IReadOnlyDictionary<string, PlannedArtifact> planned,
        List<ArtifactOperation> operations,
        List<ArtifactPublicationVerdict> verdicts,
        List<ArtifactPublicationRefusal> refusals)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var removal in plan.RequiredRemovals)
        {
            paths.Add(removal.RelativePath);
            var path = ArtifactPublicationStorage.ArtifactPath(destination, removal.RelativePath);
            var reason = Refusal(destination, removal, path, planned);
            string? currentHash = null;
            if (reason is null && File.Exists(path))
            {
                currentHash = ArtifactPublicationStorage.Hash(path);
                reason = string.Equals(ArtifactPublicationStorage.TextHash(removal.RelativePath, path), removal.ExpectedSha256, StringComparison.Ordinal)
                    ? null
                    : $"'{removal.RelativePath}' changed since the migration evidence was taken; refusing to remove it.";
            }

            if (reason is not null)
            {
                refusals.Add(new(removal.RelativePath, reason, RefusalCode));
                verdicts.Add(new(removal.RelativePath, "refused", currentHash, null, reason) { Code = RefusalCode });
            }
            else if (currentHash is not null)
            {
                operations.Add(new(ArtifactOperationKind.Delete, removal.RelativePath, true) { ExpectedSha256 = removal.ExpectedSha256 });
                verdicts.Add(new(removal.RelativePath, "delete", currentHash, null));
            }
        }

        return paths;
    }

    /// <summary>
    /// Verifies that a removal's backup still holds the content the removal was verified against.
    /// </summary>
    /// <param name="destination">The full destination path.</param>
    /// <param name="operation">The journaled operation.</param>
    /// <exception cref="UnsafeArtifactPublication">The backup is missing or does not match.</exception>
    public static void VerifyBackup(string destination, ArtifactOperation operation)
    {
        if (operation.ExpectedSha256 is not { } expected)
        {
            return;
        }

        var backup = ArtifactPublicationStorage.BackupPath(destination, operation.Path);
        if (!File.Exists(backup) ||
            !string.Equals(ArtifactPublicationStorage.TextHash(operation.Path, backup), expected, StringComparison.Ordinal))
        {
            throw new UnsafeArtifactPublication(
                $"The backup '{backup}' of required removal '{operation.Path}' does not hold the verified content, so nothing was restored. " +
                $"Inspect the destination, restore '{operation.Path}' from version control if needed, then delete the publication journal '{ArtifactPublicationStorage.JournalPath(destination)}' " +
                $"and its control directory '{ArtifactPublicationStorage.ControlPath(destination)}' before rendering again.",
                RefusalCode);
        }
    }

    /// <summary>
    /// Verifies, immediately before deleting it, that a required removal still holds the content it was verified against.
    /// </summary>
    /// <param name="operation">The journaled delete operation.</param>
    /// <param name="path">The full path of the file about to be deleted.</param>
    /// <exception cref="UnsafeArtifactPublication">The file changed since the publication was prepared.</exception>
    public static void VerifyBeforeDelete(ArtifactOperation operation, string path)
    {
        if (operation.ExpectedSha256 is { } expected &&
            !string.Equals(ArtifactPublicationStorage.TextHash(operation.Path, path), expected, StringComparison.Ordinal))
        {
            throw new UnsafeArtifactPublication(
                $"'{operation.Path}' changed during publication; refusing to remove it and rolling back.",
                RefusalCode);
        }
    }

    static string? Refusal(string destination, ArtifactRemoval removal, string path, IReadOnlyDictionary<string, PlannedArtifact> planned)
    {
        if (ArtifactPublicationStorage.SafePathRefusal(destination, removal.RelativePath) is { } unsafePath)
        {
            return unsafePath;
        }

        if (ArtifactPublicationPaths.IsReserved(removal.RelativePath) || planned.ContainsKey(removal.RelativePath))
        {
            return $"Required removal '{removal.RelativePath}' is reserved or also planned as an artifact.";
        }

        return Directory.Exists(path) ? $"Required removal '{removal.RelativePath}' is a directory." : null;
    }
}
