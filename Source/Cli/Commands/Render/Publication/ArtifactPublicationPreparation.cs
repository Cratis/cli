// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Publication;

internal sealed record PreparedArtifactPublication(
    ArtifactManifest Manifest,
    string? PreviousManifestJson,
    IReadOnlyList<ArtifactOperation> Operations,
    IReadOnlyDictionary<string, PlannedArtifact> PlannedArtifacts,
    int Unchanged,
    string? BaseManifestSha256,
    IReadOnlyList<ArtifactPublicationChange> Changes);

internal static class ArtifactPublicationPreparation
{
    public static PreparedArtifactPublication Prepare(ArtifactPublicationRequest request)
    {
        if (request.Plan.Success)
        {
            Directory.CreateDirectory(Path.GetFullPath(request.Destination));
        }

        var analysis = Analyze(request);
        if (analysis.Refusals.Count > 0)
        {
            throw new UnsafeArtifactPublication(analysis.Refusals[0].Reason);
        }

        return analysis.Prepared!;
    }

    public static ArtifactPublicationAnalysis Analyze(ArtifactPublicationRequest request)
    {
        if (!request.Plan.Success)
        {
            return Refused("A non-publishable artifact plan cannot be committed.");
        }

        var destination = Path.GetFullPath(request.Destination);
        var manifestPath = ArtifactPublicationStorage.ManifestPath(destination);
        var previousBytes = File.Exists(manifestPath) ? File.ReadAllBytes(manifestPath) : null;
        var previousJson = previousBytes is null ? null : ArtifactPublicationStorage.DecodeManifest(previousBytes);
        ArtifactManifest? previous;
        try
        {
            previous = previousJson is null ? null : ArtifactPublicationStorage.ParseManifest(previousJson);
        }
        catch (UnsafeArtifactPublication exception)
        {
            return Refused(exception.Message);
        }

        var next = ArtifactManifest.From(request.Plan);
        var refusals = new List<ArtifactPublicationRefusal>();
        var manifestReason = ManifestRefusal(previous, next);
        if (manifestReason is not null)
        {
            refusals.Add(new(null, manifestReason));
        }

        refusals.AddRange(ArtifactPublicationPaths.Analyze(destination, previous?.Artifacts ?? [], next.Artifacts));
        var unsafePaths = refusals.Select(_ => _.Path).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var verdicts = refusals.ConvertAll(_ => new ArtifactPublicationVerdict(_.Path, "refused", null, null, _.Reason));
        var planned = request.Plan.Artifacts.ToDictionary(_ => _.RelativePath, StringComparer.Ordinal);
        var previousByPath = (previous?.Artifacts ?? []).ToDictionary(_ => _.Path, StringComparer.Ordinal);
        var operations = new List<ArtifactOperation>();

        foreach (var artifact in next.Artifacts.Where(_ => !unsafePaths.Contains(_.Path)))
        {
            var path = ArtifactPublicationStorage.ArtifactPath(destination, artifact.Path);
            var owned = previousByPath.GetValueOrDefault(artifact.Path);
            var exists = File.Exists(path);

            // An unmanaged file is refused without reading it.
            var currentHash = exists && owned is not null ? ArtifactPublicationStorage.Hash(path) : null;
            var reason = ActiveRefusal(artifact.Path, path, owned, exists, currentHash, request.Force);
            if (reason is not null)
            {
                refusals.Add(new(artifact.Path, reason));
                verdicts.Add(new(artifact.Path, "refused", currentHash, artifact.Sha256, reason));
            }
            else if (string.Equals(currentHash, artifact.Sha256, StringComparison.Ordinal))
            {
                verdicts.Add(new(artifact.Path, "unchanged", currentHash, artifact.Sha256));
            }
            else
            {
                operations.Add(new(ArtifactOperationKind.Write, artifact.Path, exists));
                verdicts.Add(new(artifact.Path, "write", currentHash, artifact.Sha256));
            }
        }

        foreach (var stale in previousByPath.Values.Where(_ => !planned.ContainsKey(_.Path) && !unsafePaths.Contains(_.Path)))
        {
            var path = ArtifactPublicationStorage.ArtifactPath(destination, stale.Path);
            if (!File.Exists(path))
            {
                continue;
            }

            var currentHash = ArtifactPublicationStorage.Hash(path);
            if (!string.Equals(currentHash, stale.Sha256, StringComparison.Ordinal))
            {
                var reason = $"Stale managed artifact '{stale.Path}' was modified and will not be removed.";
                refusals.Add(new(stale.Path, reason));
                verdicts.Add(new(stale.Path, "refused", currentHash, null, reason));
            }
            else
            {
                operations.Add(new(ArtifactOperationKind.Delete, stale.Path, true));
                verdicts.Add(new(stale.Path, "delete", currentHash, null));
            }
        }

        var changes = verdicts.Where(_ => _.Kind == "write" || _.Kind == "delete")
            .Select(_ => new ArtifactPublicationChange(_.Path!, _.Kind, _.BeforeSha256, _.AfterSha256)).ToArray();
        var prepared = new PreparedArtifactPublication(
            next,
            previousJson,
            operations,
            planned,
            verdicts.Count(_ => _.Kind == "unchanged"),
            previousBytes is null ? null : ArtifactPublicationStorage.Hash(previousBytes),
            changes);

        return new(prepared, verdicts, refusals);
    }

    static ArtifactPublicationAnalysis Refused(string reason) =>
        new(null, [new(null, "refused", null, null, reason)], [new(null, reason)]);

    static string? ActiveRefusal(string relativePath, string path, ManagedArtifact? owned, bool exists, string? currentHash, bool force)
    {
        if (Directory.Exists(path))
        {
            return $"Artifact '{relativePath}' collides with an existing directory.";
        }

        if (!exists)
        {
            return null;
        }

        if (owned is null)
        {
            return $"Artifact '{relativePath}' is an unmanaged existing file.";
        }

        return !string.Equals(currentHash, owned.Sha256, StringComparison.Ordinal) && !force
            ? $"Managed artifact '{relativePath}' was modified by the user; pass --force to replace it."
            : null;
    }

    static string? ManifestRefusal(ArtifactManifest? previous, ArtifactManifest next)
    {
        if (next.SchemaVersion != ArtifactManifest.CurrentSchemaVersion ||
            next.ArtifactPlanSchemaVersion != ArtifactRenderPlan.CurrentSchemaVersion)
        {
            return "The artifact or ownership manifest schema requires an explicit migration.";
        }

        if (previous is not null && (previous.SchemaVersion != ArtifactManifest.CurrentSchemaVersion ||
            previous.ArtifactPlanSchemaVersion != next.ArtifactPlanSchemaVersion ||
            previous.Target != next.Target || previous.Renderer != next.Renderer ||
            previous.ApplicationName != next.ApplicationName))
        {
            return "The existing manifest identity or schema requires an explicit migration.";
        }

        return null;
    }
}
