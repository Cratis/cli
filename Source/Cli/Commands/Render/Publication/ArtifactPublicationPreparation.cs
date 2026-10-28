// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render.Publication;

internal sealed record PreparedArtifactPublication(
    ArtifactManifest Manifest,
    string? PreviousManifestJson,
    IReadOnlyList<ArtifactOperation> Operations,
    IReadOnlyDictionary<string, PlannedArtifact> PlannedArtifacts,
    int Unchanged,
    string? BaseManifestSha256,
    IReadOnlyList<ArtifactPublicationChange> Changes)
{
    public IReadOnlyList<string> Orphaned { get; init; } = [];
}

internal static class ArtifactPublicationPreparation
{
    public static PreparedArtifactPublication Prepare(ArtifactPublicationRequest request)
    {
        var analysis = Analyze(request);
        if (analysis.Refusals.Count > 0)
        {
            throw new UnsafeArtifactPublication(analysis.Refusals[0].Reason, analysis.Refusals[0].Code);
        }

        Directory.CreateDirectory(Path.GetFullPath(request.Destination));

        return analysis.Prepared!;
    }

    public static ArtifactPublicationAnalysis Analyze(ArtifactPublicationRequest request)
    {
        if (!request.Plan.CanPublishArtifacts)
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

        var publicationRefusal = CheckPublication(request.Plan, destination);
        if (publicationRefusal is not null)
        {
            return publicationRefusal;
        }

        if (request.Selection is not null)
        {
            var missing = MissingDependencies(request, previous);
            if (missing.Count > 0)
            {
                return Refused($"Missing managed declarations outside the scope: {string.Join("; ", missing)}.", "CLI-RENDER-014");
            }
        }

        var artifacts = request.Plan.Artifacts;
        if (request.Selection is not null && previous is null && request.ScaffoldOptions is { } options)
        {
            var scaffold = CratisRendering.PlanScaffold(options);
            if (!scaffold.Success)
            {
                return Refused(string.Join("; ", scaffold.Diagnostics.Select(diagnostic => diagnostic.Message)), "CLI-RENDER-002");
            }
            artifacts = [.. artifacts.Concat(scaffold.Artifacts).OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)];
        }

        var next = ArtifactManifest.From(request.Plan, request.SemanticAddresses, artifacts);
        var retained = request.Selection is null ? [] : (previous?.Artifacts ?? []).Where(artifact =>
            !request.Plan.Artifacts.Any(planned => planned.RelativePath == artifact.Path) && !IsSelected(artifact, request)).ToArray();
        if (request.Selection is not null)
        {
            next = next with { Artifacts = [.. next.Artifacts.Concat(retained).OrderBy(artifact => artifact.Path, StringComparer.Ordinal)] };
        }
        var refusals = new List<ArtifactPublicationRefusal>();
        var manifestReason = ManifestRefusal(previous, next);
        if (manifestReason is not null)
        {
            refusals.Add(new(null, manifestReason));
        }

        refusals.AddRange(ArtifactPublicationPaths.Analyze(destination, previous?.Artifacts ?? [], next.Artifacts));
        var unsafePaths = refusals.Select(_ => _.Path).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var verdicts = refusals.ConvertAll(_ => new ArtifactPublicationVerdict(_.Path, "refused", null, null, _.Reason));
        var planned = artifacts.ToDictionary(_ => _.RelativePath, StringComparer.Ordinal);
        var previousByPath = (previous?.Artifacts ?? []).ToDictionary(_ => _.Path, StringComparer.Ordinal);
        var operations = new List<ArtifactOperation>();
        var removals = ArtifactPublicationRemovals.Analyze(destination, request.Plan, planned, operations, verdicts, refusals);

        foreach (var artifact in next.Artifacts.Where(_ => planned.ContainsKey(_.Path) && !unsafePaths.Contains(_.Path)))
        {
            var path = ArtifactPublicationStorage.ArtifactPath(destination, artifact.Path);
            var owned = previousByPath.GetValueOrDefault(artifact.Path);
            var exists = File.Exists(path);

            // An unmanaged file is refused without hashing it; Stage's publication check may already have read it for event type ids.
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

        foreach (var stale in previousByPath.Values.Where(_ => !planned.ContainsKey(_.Path) && !unsafePaths.Contains(_.Path) && !removals.Contains(_.Path) &&
            (request.Selection is null || IsSelected(_, request))))
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

        verdicts = verdicts.ConvertAll(verdict => verdict with
        {
            Sources = ResolveSources(verdict.Path, planned, previousByPath, request.SemanticAddresses)
        });
        var changes = verdicts.Where(_ => _.Kind == "write" || _.Kind == "delete")
            .Select(_ => new ArtifactPublicationChange(_.Path!, _.Kind, _.BeforeSha256, _.AfterSha256) { Sources = _.Sources }).ToArray();
        var prepared = new PreparedArtifactPublication(
            next,
            previousJson,
            operations,
            planned,
            verdicts.Count(_ => _.Kind == "unchanged"),
            previousBytes is null ? null : ArtifactPublicationStorage.Hash(previousBytes),
            changes)
        {
            Orphaned = [.. retained.Where(artifact => artifact.Sources.Any(id => request.SemanticAddresses.Resolve(id).Kind == "unknown"))
                .Select(artifact => artifact.Path).Order(StringComparer.Ordinal)]
        };

        return new(prepared, verdicts, refusals);
    }

    static IReadOnlyList<ArtifactSource> ResolveSources(
        string? path,
        Dictionary<string, PlannedArtifact> planned,
        IReadOnlyDictionary<string, ManagedArtifact> previous,
        SemanticAddressIndex addresses)
    {
        if (path is null)
        {
            return [];
        }

        return planned.TryGetValue(path, out var artifact)
            ? [.. artifact.Sources.Select(id => addresses.Resolve(id.ToString()))]
            : [.. (previous.GetValueOrDefault(path)?.Sources ?? []).Select(addresses.Resolve)];
    }

    static ArtifactPublicationAnalysis Refused(string reason, string? code = null) =>
        new(null, [new(null, "refused", null, null, reason) { Code = code }], [new(null, reason, code)]);

    static bool IsSelected(ManagedArtifact artifact, ArtifactPublicationRequest request) =>
        (artifact.Owners is { Count: > 0 } chains && chains.All(chain =>
            request.Selection!.Scopes.Any(scope => chain.Contains(scope.Artifact.ToString(), StringComparer.Ordinal)))) ||
        ((artifact.Owners is null || artifact.Owners.Count == 0) && artifact.Sources.Count > 0 &&
            artifact.Sources.All(id => request.Selection!.Contains(id, request.SemanticAddresses)));

    static ArtifactPublicationAnalysis? CheckPublication(ArtifactRenderPlan plan, string destination)
    {
        CratisPublicationCheck check;
        try
        {
            check = CratisRendering.CheckPublication(plan, path => ReadExisting(destination, path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Refused($"An existing destination file could not be read for the publication check: {exception.Message}");
        }

        return check switch
        {
            CratisPublicationCheck.Compatible => null,
            CratisPublicationCheck.RequiresApplicationScope required => Refused(
                $"Incompatible policy layout: {string.Join(", ", required.Paths)}. Render the whole application once before rendering a scope.",
                "CLI-RENDER-013"),
            CratisPublicationCheck.UnsafeRemovals removals => Refused(
                $"Unsafe reducer migration removals: {string.Join("; ", removals.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"))}",
                ArtifactPublicationRemovals.RefusalCode),
            CratisPublicationCheck.ChangesEventTypeIds eventIds => Refused(
                string.Join(' ', new[]
                {
                    $"Stored event type ids would change or could not be determined: {string.Join("; ", eventIds.Changes.Select(change => $"{change.Path}: {change.TypeName}: {change.ExistingId} -> {change.PlannedId ?? "<unknown>"}"))}.",
                    string.Join("; ", eventIds.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")),
                    "Pin the event's 'id' to keep its stored event type id."
                }.Where(part => part.Length > 0)),
                "CLI-RENDER-016"),
            _ => Refused("The Stage publication check did not confirm compatibility.")
        };
    }

    static string? ReadExisting(string destination, string relativePath)
    {
        ArtifactPublicationStorage.EnsureSafePath(destination, relativePath);
        try
        {
            return File.ReadAllText(ArtifactPublicationStorage.ArtifactPath(destination, relativePath));
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    static IReadOnlyList<string> MissingDependencies(ArtifactPublicationRequest request, ArtifactManifest? previous)
    {
        var available = (previous?.Artifacts ?? []).SelectMany(artifact => artifact.Sources)
            .Concat(request.Plan.Artifacts.SelectMany(artifact => artifact.Sources.Select(id => id.ToString()))
                .Where(id => request.Selection!.Contains(id, request.SemanticAddresses) ||
                    string.Equals(request.SemanticAddresses.Resolve(id).Kind, "eventsource", StringComparison.Ordinal) ||
                    string.Equals(request.SemanticAddresses.Resolve(id).Kind, "stream", StringComparison.Ordinal)))
            .ToHashSet(StringComparer.Ordinal);

        return [.. RenderDependencies.Collect(request.Selection!, request.SemanticAddresses).Where(source => !available.Contains(source.Id))
            .Select(source => $"'{source.Address!.Replace('/', '.')}' ({DependencyAdvice(source, request.SemanticAddresses)})")];
    }

    static string DependencyAdvice(ArtifactSource source, SemanticAddressIndex index)
    {
        var owners = index.Owners(source.Id);
        var slice = owners.Count == 0 ? null : owners[^1];

        return slice is null ? "render the whole application once" : $"add --scope {index.Resolve(slice).Address!.Replace('/', '.')}";
    }

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

        if (previous is not null && ((previous.SchemaVersion != ArtifactManifest.CurrentSchemaVersion && previous.SchemaVersion != "1") ||
            previous.ArtifactPlanSchemaVersion != next.ArtifactPlanSchemaVersion ||
            previous.Target != next.Target || previous.Renderer != next.Renderer ||
            previous.ApplicationName != next.ApplicationName))
        {
            return "The existing manifest identity or schema requires an explicit migration.";
        }

        return null;
    }
}
