// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Derives reducer migration evidence from the destination's managed classic reducer files, without changing them.
/// </summary>
internal static class CratisReducerMigration
{
    /// <summary>
    /// The diagnostic code for a read model that cannot be migrated from the destination.
    /// </summary>
    public const string RefusalCode = "CLI-RENDER-026";

    const string ClassicReducerMarker = "IReducerFor<";

    /// <summary>
    /// Resolves migration evidence for each requested read model.
    /// </summary>
    /// <param name="readModels">The requested read models, by dotted address or semantic id.</param>
    /// <param name="destination">The publication destination.</param>
    /// <param name="addresses">The model's semantic addresses.</param>
    /// <returns>The evidence for read models still built by a classic reducer, or the first refusal.</returns>
    public static (IReadOnlyList<ReducerMigrationEvidence> Evidence, ScreenplayDiagnostic? Error) Resolve(
        IReadOnlyList<string> readModels,
        string? destination,
        SemanticAddressIndex addresses)
    {
        if (readModels.Count == 0)
        {
            return ([], null);
        }

        if (destination is null)
        {
            return Refused("--migrate-reducer needs a destination that holds the rendered classic reducers.", null);
        }

        try
        {
            var manifest = ArtifactPublicationStorage.ReadManifest(destination);
            if (manifest is null)
            {
                return Refused("--migrate-reducer needs a rendered application; the destination has no ownership manifest.", null);
            }

            var migrated = MigratedReadModels(destination);
            var evidence = new List<ReducerMigrationEvidence>();
            foreach (var requested in readModels.Distinct(StringComparer.Ordinal))
            {
                var readModel = addresses.Sources.SingleOrDefault(source => source.Kind == "readmodel" &&
                    (source.Id == requested || string.Equals(source.Address, requested.Replace('.', '/'), StringComparison.Ordinal)));
                if (readModel is null)
                {
                    return Refused($"'{requested}' is not a read model in this application. Name it as Module.Feature.Slice.ReadModel or by its semantic id.", requested);
                }

                if (migrated.Contains(readModel.Id))
                {
                    continue;
                }

                var found = ClassicReducers(destination, manifest, addresses.Identity(readModel.Id), readModel);
                if (found.Count != 1)
                {
                    return Refused(
                        found.Count == 0
                            ? $"No managed classic reducer for read model '{requested}' was found in the destination."
                            : $"Read model '{requested}' is built by {found.Count} managed classic reducers ({string.Join(", ", found.Select(_ => _.RelativePath))}); only a single reducer can be migrated.",
                        requested);
                }

                if (found[0].Refusal is { } refusal)
                {
                    return Refused($"Read model '{requested}' cannot be migrated from '{found[0].RelativePath}': {refusal}", requested);
                }

                var owned = manifest.Artifacts.Single(artifact => artifact.Path == found[0].RelativePath);
                if (!string.Equals(ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ArtifactPath(destination, owned.Path)), owned.Sha256, StringComparison.Ordinal))
                {
                    return Refused($"The managed classic reducer '{owned.Path}' of read model '{requested}' was modified. Restore it before migrating; migration deletes it, and --force never authorizes deleting a modified file.", requested);
                }

                evidence.Add(found[0]);
            }

            return (evidence, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or UnsafeArtifactPublication)
        {
            return Refused($"The destination's classic reducers cannot be read: {exception.Message}", null);
        }
    }

    static List<ReducerMigrationEvidence> ClassicReducers(string destination, ArtifactManifest manifest, SemanticId id, ArtifactSource readModel)
    {
        var separator = readModel.Address!.LastIndexOf('/');
        var name = readModel.Address[(separator + 1)..];
        var slice = separator < 0 ? string.Empty : $"/{readModel.Address[..separator]}/";
        var found = new List<ReducerMigrationEvidence>();
        foreach (var artifact in manifest.Artifacts.Where(_ => _.Path.EndsWith(".cs", StringComparison.Ordinal)))
        {
            ArtifactPublicationStorage.EnsureSafePath(destination, artifact.Path);
            var path = ArtifactPublicationStorage.ArtifactPath(destination, artifact.Path);
            if (!File.Exists(path))
            {
                continue;
            }

            var content = File.ReadAllText(path);
            if (!content.Contains(ClassicReducerMarker, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var evidence = ReducerMigrationEvidence.FromExistingFile(id, artifact.Path, content);
                if (IsTypeNamed(evidence.ReadModelType, name) && IsInSlice(artifact.Path, slice))
                {
                    found.Add(evidence);
                }
            }
            catch (InvalidArtifactRenderContract)
            {
                // Not exactly one classic reducer class: it cannot be the reducer this read model migrates from.
            }
        }

        return found;
    }

    static bool IsTypeNamed(string typeName, string name)
    {
        var simple = typeName[(typeName.LastIndexOfAny(['.', '+', ':']) + 1)..];
        return string.Equals(simple, name, StringComparison.Ordinal);
    }

    static bool IsInSlice(string path, string slice) =>
        slice.Length == 0 || $"/{path}".Contains(slice, StringComparison.Ordinal);

    static HashSet<string> MigratedReadModels(string destination)
    {
        ArtifactPublicationStorage.EnsureSafePath(destination, ReducerConventionInput.RelativePath);
        var path = ArtifactPublicationStorage.ArtifactPath(destination, ReducerConventionInput.RelativePath);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return ids;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("readModels", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entries.EnumerateArray())
                {
                    if (entry.TryGetProperty("id", out var id) && id.GetString() is { } value)
                    {
                        ids.Add(value);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // The reducer convention resolution reports a malformed configuration before migration runs.
        }

        return ids;
    }

    static (IReadOnlyList<ReducerMigrationEvidence> Evidence, ScreenplayDiagnostic? Error) Refused(string message, string? location) =>
        ([], new(ScreenplayDiagnosticSeverity.Error, RefusalCode, message, location));
}
