// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render.Publication;

internal sealed record PreparedArtifactPublication(
    ArtifactManifest Manifest,
    IReadOnlyList<ArtifactOperation> Operations,
    IReadOnlyDictionary<string, PlannedArtifact> PlannedArtifacts,
    int Unchanged)
{
    public string? PreviousManifestJson { get; init; }
    public string? BaseManifestSha256 { get; init; }
    public IReadOnlyList<ArtifactPublicationChange> Changes { get; init; } = [];
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

        return CheckPublication(request, destination) ?? AnalyzePlan(request, new(previousJson, previousBytes, previous));
    }

    static ArtifactPublicationAnalysis AnalyzePlan(ArtifactPublicationRequest request, PreviousPublication previous)
    {
        var missing = request.Selection is null ? [] : MissingDependencies(request, previous.Manifest);
        if (missing.Count > 0)
        {
            return Refused($"Missing managed declarations outside the scope: {string.Join("; ", missing)}.", "CLI-RENDER-014");
        }

        var artifacts = request.Plan.Artifacts;
        if (FreshScaffoldFor(request, previous.Manifest) is { } scaffold)
        {
            if (!scaffold.Success)
            {
                return Refused(string.Join("; ", scaffold.Diagnostics.Select(diagnostic => diagnostic.Message)), "CLI-RENDER-002");
            }
            artifacts = [.. artifacts.Concat(scaffold.Artifacts).OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)];
        }

        var retained = RetainedArtifacts(request, previous.Manifest);
        var next = NextManifest(request, artifacts, retained);
        return AnalyzeArtifacts(request, previous, new(next, artifacts, retained));
    }

    static ManagedArtifact[] RetainedArtifacts(ArtifactPublicationRequest request, ArtifactManifest? previous) =>
        request.Selection is null ? [] : [.. (previous?.Artifacts ?? []).Where(artifact =>
            !request.Plan.Artifacts.Any(planned => planned.RelativePath == artifact.Path) && !IsSelected(artifact, request))];

    static ArtifactManifest NextManifest(ArtifactPublicationRequest request, ImmutableArray<PlannedArtifact> artifacts, ManagedArtifact[] retained)
    {
        var next = ArtifactManifest.From(request.Plan, request.SemanticAddresses, artifacts);
        return request.Selection is null ? next : next with { Artifacts = [.. next.Artifacts.Concat(retained).OrderBy(artifact => artifact.Path, StringComparer.Ordinal)] };
    }

    static ArtifactPublicationAnalysis AnalyzeArtifacts(ArtifactPublicationRequest request, PreviousPublication previous, NextPublication next)
    {
        var destination = Path.GetFullPath(request.Destination);
        var refusals = new List<ArtifactPublicationRefusal>();
        var manifestReason = ManifestRefusal(previous.Manifest, next.Manifest);
        if (manifestReason is not null)
        {
            refusals.Add(new(null, manifestReason));
        }

        refusals.AddRange(ArtifactPublicationPaths.Analyze(destination, previous.Manifest?.Artifacts ?? [], next.Manifest.Artifacts));
        var unsafePaths = refusals.Select(_ => _.Path).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var verdicts = refusals.ConvertAll(_ => new ArtifactPublicationVerdict(_.Path, "refused", null, null, _.Reason));
        var planned = next.Artifacts.ToDictionary(_ => _.RelativePath, StringComparer.Ordinal);
        var previousByPath = (previous.Manifest?.Artifacts ?? []).ToDictionary(_ => _.Path, StringComparer.Ordinal);
        var decisions = new ArtifactDecisions(planned, previousByPath, verdicts, refusals);
        var removals = ArtifactPublicationRemovals.Analyze(destination, request.Plan, planned, decisions.Operations, verdicts, refusals);
        AnalyzeActive(destination, request.Force, next.Manifest.Artifacts.Where(_ => planned.ContainsKey(_.Path) && !unsafePaths.Contains(_.Path)), decisions);
        var excluded = unsafePaths.Concat(removals).Concat(planned.Keys).ToHashSet(StringComparer.Ordinal);
        AnalyzeStale(
            destination,
            previousByPath.Values.Where(_ => !excluded.Contains(_.Path) && (request.Selection is null || IsSelected(_, request))),
            decisions);
        return CompleteAnalysis(request, previous, next, decisions);
    }

    static void AnalyzeActive(string destination, bool force, IEnumerable<ManagedArtifact> artifacts, ArtifactDecisions decisions)
    {
        foreach (var artifact in artifacts)
        {
            var path = ArtifactPublicationStorage.ArtifactPath(destination, artifact.Path);
            var owned = decisions.Previous.GetValueOrDefault(artifact.Path);
            var exists = File.Exists(path);

            // An unmanaged file is refused without hashing it; Stage's publication check may already have read it for event type ids.
            var currentHash = exists && owned is not null ? ArtifactPublicationStorage.Hash(path) : null;
            var reason = ActiveRefusal(new(artifact.Path, path) { Owned = owned, Exists = exists, Hash = currentHash }, force);
            if (reason is not null)
            {
                decisions.Refusals.Add(new(artifact.Path, reason));
                decisions.Verdicts.Add(new(artifact.Path, "refused", currentHash, artifact.Sha256, reason));
            }
            else if (string.Equals(currentHash, artifact.Sha256, StringComparison.Ordinal))
            {
                decisions.Verdicts.Add(new(artifact.Path, "unchanged", currentHash, artifact.Sha256));
            }
            else
            {
                decisions.Operations.Add(new(ArtifactOperationKind.Write, artifact.Path, exists));
                decisions.Verdicts.Add(new(artifact.Path, "write", currentHash, artifact.Sha256));
            }
        }
    }

    static void AnalyzeStale(string destination, IEnumerable<ManagedArtifact> artifacts, ArtifactDecisions decisions)
    {
        foreach (var stale in artifacts)
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
                decisions.Refusals.Add(new(stale.Path, reason));
                decisions.Verdicts.Add(new(stale.Path, "refused", currentHash, null, reason));
            }
            else
            {
                decisions.Operations.Add(new(ArtifactOperationKind.Delete, stale.Path, true));
                decisions.Verdicts.Add(new(stale.Path, "delete", currentHash, null));
            }
        }
    }

    static ArtifactPublicationAnalysis CompleteAnalysis(ArtifactPublicationRequest request, PreviousPublication previous, NextPublication next, ArtifactDecisions decisions)
    {
        var verdicts = decisions.Verdicts.ConvertAll(verdict => verdict with
        {
            Sources = ResolveSources(verdict.Path, decisions.Planned, decisions.Previous, request.SemanticAddresses)
        });
        var changes = verdicts.Where(_ => _.Kind == "write" || _.Kind == "delete")
            .Select(_ => new ArtifactPublicationChange(_.Path!, _.Kind, _.BeforeSha256, _.AfterSha256) { Sources = _.Sources }).ToArray();
        var prepared = new PreparedArtifactPublication(
            next.Manifest,
            decisions.Operations,
            decisions.Planned,
            verdicts.Count(_ => _.Kind == "unchanged"))
        {
            PreviousManifestJson = previous.Json,
            BaseManifestSha256 = previous.Bytes is null ? null : ArtifactPublicationStorage.Hash(previous.Bytes),
            Changes = changes,
            Orphaned = [.. next.Retained.Where(artifact => artifact.Sources.Any(id => request.SemanticAddresses.Resolve(id).Kind == "unknown"))
                .Select(artifact => artifact.Path).Order(StringComparer.Ordinal)]
        };

        return new(prepared, verdicts, decisions.Refusals);
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

    static CratisPlanResult? FreshScaffoldFor(ArtifactPublicationRequest request, ArtifactManifest? previous) =>
        request.Selection is not null && previous is null && request.ScaffoldOptions is { } options
            ? request.Target.PlanScaffold(options)
            : null;

    static ArtifactPublicationAnalysis? CheckPublication(ArtifactPublicationRequest request, string destination)
    {
        try
        {
            var refusal = request.Target.CheckPublication(request.Plan, path => ReadExisting(destination, path));
            return refusal is null ? null : Refused(refusal.Reason, refusal.Code);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Refused($"An existing destination file could not be read for the publication check: {exception.Message}");
        }
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

    static string? ActiveRefusal(ExistingArtifact artifact, bool force)
    {
        if (Directory.Exists(artifact.Path))
        {
            return $"Artifact '{artifact.RelativePath}' collides with an existing directory.";
        }

        if (!artifact.Exists)
        {
            return null;
        }

        if (artifact.Owned is null)
        {
            return $"Artifact '{artifact.RelativePath}' is an unmanaged existing file.";
        }

        return !string.Equals(artifact.Hash, artifact.Owned.Sha256, StringComparison.Ordinal) && !force
            ? $"Managed artifact '{artifact.RelativePath}' was modified by the user; pass --force to replace it."
            : null;
    }

    static string? ManifestRefusal(ArtifactManifest? previous, ArtifactManifest next)
    {
        if (next.SchemaVersion != ArtifactManifest.CurrentSchemaVersion ||
            next.ArtifactPlanSchemaVersion != ArtifactRenderPlan.CurrentSchemaVersion)
        {
            return "The artifact or ownership manifest schema requires an explicit migration.";
        }

        if (previous is not null && !CompatibleManifest(previous, next))
        {
            return "The existing manifest identity or schema requires an explicit migration.";
        }

        return null;
    }

    static bool CompatibleManifest(ArtifactManifest previous, ArtifactManifest next) =>
        SupportedPreviousSchema(previous) && SameManifestIdentity(previous, next);

    static bool SupportedPreviousSchema(ArtifactManifest previous) =>
        previous.SchemaVersion == ArtifactManifest.CurrentSchemaVersion || previous.SchemaVersion == "1";

    static bool SameManifestIdentity(ArtifactManifest previous, ArtifactManifest next) =>
        previous.ArtifactPlanSchemaVersion == next.ArtifactPlanSchemaVersion &&
        previous.Target == next.Target && previous.Renderer == next.Renderer &&
        previous.ApplicationName == next.ApplicationName;

    sealed record PreviousPublication(string? Json, byte[]? Bytes, ArtifactManifest? Manifest);

    sealed record NextPublication(ArtifactManifest Manifest, ImmutableArray<PlannedArtifact> Artifacts, ManagedArtifact[] Retained);

    sealed record ArtifactDecisions(
        Dictionary<string, PlannedArtifact> Planned,
        Dictionary<string, ManagedArtifact> Previous,
        List<ArtifactPublicationVerdict> Verdicts,
        List<ArtifactPublicationRefusal> Refusals)
    {
        public List<ArtifactOperation> Operations { get; } = [];
    }

    sealed record ExistingArtifact(string RelativePath, string Path)
    {
        public ManagedArtifact? Owned { get; init; }
        public bool Exists { get; init; }
        public string? Hash { get; init; }
    }
}
