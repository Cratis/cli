// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render.Publication;

internal sealed record PreparedArtifactPublication(
    ArtifactManifest Manifest,
    string? PreviousManifestJson,
    IReadOnlyList<ArtifactOperation> Operations,
    IReadOnlyDictionary<string, PlannedArtifact> PlannedArtifacts,
    int Unchanged);

internal static class ArtifactPublicationPreparation
{
    public static PreparedArtifactPublication Prepare(ArtifactPublicationRequest request)
    {
        if (!request.Plan.Success)
        {
            throw new UnsafeArtifactPublication("A non-publishable artifact plan cannot be committed.");
        }

        var destination = Path.GetFullPath(request.Destination);
        Directory.CreateDirectory(destination);
        var previousJson = ArtifactPublicationStorage.ReadManifestJson(destination);
        var previous = previousJson is null ? null : ArtifactPublicationStorage.ReadManifest(destination);
        var next = ArtifactManifest.From(request.Plan);
        ValidateManifest(previous, next);
        ValidatePaths(destination, previous?.Artifacts ?? [], next.Artifacts);

        var planned = request.Plan.Artifacts.ToDictionary(_ => _.RelativePath, StringComparer.Ordinal);
        var previousByPath = (previous?.Artifacts ?? []).ToDictionary(_ => _.Path, StringComparer.Ordinal);
        var operations = new List<ArtifactOperation>();
        var unchanged = 0;

        foreach (var artifact in next.Artifacts)
        {
            var path = ArtifactPublicationStorage.ArtifactPath(destination, artifact.Path);
            if (Directory.Exists(path))
            {
                throw new UnsafeArtifactPublication($"Artifact '{artifact.Path}' collides with an existing directory.");
            }

            if (!File.Exists(path))
            {
                operations.Add(new(ArtifactOperationKind.Write, artifact.Path, false));
                continue;
            }

            if (!previousByPath.TryGetValue(artifact.Path, out var owned))
            {
                throw new UnsafeArtifactPublication($"Artifact '{artifact.Path}' is an unmanaged existing file.");
            }

            var currentHash = ArtifactPublicationStorage.Hash(path);
            if (!string.Equals(currentHash, owned.Sha256, StringComparison.Ordinal) && !request.Force)
            {
                throw new UnsafeArtifactPublication($"Managed artifact '{artifact.Path}' was modified by the user; pass --force to replace it.");
            }

            if (string.Equals(currentHash, artifact.Sha256, StringComparison.Ordinal))
            {
                unchanged++;
            }
            else
            {
                operations.Add(new(ArtifactOperationKind.Write, artifact.Path, true));
            }
        }

        foreach (var stale in previousByPath.Values.Where(_ => !planned.ContainsKey(_.Path)))
        {
            var path = ArtifactPublicationStorage.ArtifactPath(destination, stale.Path);
            if (!File.Exists(path))
            {
                continue;
            }

            if (!string.Equals(ArtifactPublicationStorage.Hash(path), stale.Sha256, StringComparison.Ordinal))
            {
                throw new UnsafeArtifactPublication($"Stale managed artifact '{stale.Path}' was modified and will not be removed.");
            }

            operations.Add(new(ArtifactOperationKind.Delete, stale.Path, true));
        }

        return new(next, previousJson, operations, planned, unchanged);
    }

    static void ValidateManifest(ArtifactManifest? previous, ArtifactManifest next)
    {
        if (next.SchemaVersion != ArtifactManifest.CurrentSchemaVersion ||
            next.ArtifactPlanSchemaVersion != ArtifactRenderPlan.CurrentSchemaVersion)
        {
            throw new UnsafeArtifactPublication("The artifact or ownership manifest schema requires an explicit migration.");
        }

        if (previous is null)
        {
            return;
        }

        if (previous.SchemaVersion != ArtifactManifest.CurrentSchemaVersion ||
            previous.ArtifactPlanSchemaVersion != next.ArtifactPlanSchemaVersion ||
            previous.Target != next.Target || previous.Renderer != next.Renderer ||
            previous.ApplicationName != next.ApplicationName)
        {
            throw new UnsafeArtifactPublication("The existing manifest identity or schema requires an explicit migration.");
        }
    }

    static void ValidatePaths(
        string destination,
        IReadOnlyList<ManagedArtifact> previous,
        IReadOnlyList<ManagedArtifact> next)
    {
        var paths = previous.Select(_ => _.Path).Concat(next.Select(_ => _.Path)).ToArray();
        if (paths.Any(IsReserved) || paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Distinct(StringComparer.Ordinal).Count())
        {
            throw new UnsafeArtifactPublication("The manifest contains a reserved or case-colliding artifact path.");
        }

        foreach (var path in paths.Distinct(StringComparer.Ordinal))
        {
            ArtifactPublicationStorage.EnsureSafePath(destination, path);
        }
    }

    static bool IsReserved(string path) => path.Equals(ArtifactPublicationStorage.ManifestFileName, StringComparison.OrdinalIgnoreCase) ||
        path.Equals(ArtifactPublicationStorage.ControlDirectoryName, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith($"{ArtifactPublicationStorage.ControlDirectoryName}/", StringComparison.OrdinalIgnoreCase);
}
