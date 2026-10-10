// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_receipting_replacements_and_deletions : given.an_artifact_publication
{
    const string StalePath = "stale.cs";
    readonly Dictionary<string, string> _before = new(StringComparer.Ordinal);
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationChange[] _expected = [];
    SemanticAddressIndex _semanticAddresses = null!;
    string _baseManifestHash = null!;

    async Task Establish()
    {
        await Publish();
        foreach (var artifact in _plan.Artifacts)
        {
            _before.Add(artifact.RelativePath, ArtifactPublicationStorage.Hash(ArtifactPath(artifact.RelativePath)));
        }

        await File.WriteAllTextAsync(ArtifactPath(StalePath), "owned stale content");
        _before.Add(StalePath, ArtifactPublicationStorage.Hash(ArtifactPath(StalePath)));
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        ArtifactPublicationStorage.WriteDurable(
            ArtifactPublicationStorage.ManifestPath(_destination),
            ArtifactPublicationStorage.Serialize(manifest with { Artifacts = [.. manifest.Artifacts, new ManagedArtifact(StalePath, _before[StalePath]) { Sources = ["removed-id"] }] }));
        _baseManifestHash = ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination));
        await File.WriteAllTextAsync(_file, Source.Replace("Screenplay", "Updated project", StringComparison.Ordinal));
        var planned = await Plan();
        _plan = planned.Artifacts!;
        _semanticAddresses = planned.SemanticAddresses;
        _expected =
        [
            .. _plan.Artifacts.Where(artifact => artifact.Sha256 != _before[artifact.RelativePath])
                .Select(artifact => new ArtifactPublicationChange(artifact.RelativePath, "write", _before[artifact.RelativePath], artifact.Sha256)),
            new(StalePath, "delete", _before[StalePath], null)
        ];
    }

    async Task Because() => _result = await _publisher.Publish(new(_plan, _destination, false) { SemanticAddresses = _semanticAddresses }, CancellationToken.None);

    [Fact] void should_exercise_actual_replacements() => _expected.Length.ShouldBeGreaterThan(1);
    [Fact] void should_report_plan_ordered_writes_then_the_owned_deletion() => _result.Receipt.Changes.Select(change => change with { Sources = [] }).SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_report_only_changed_artifacts() => _result.Written.ShouldEqual(_expected.Length - 1);
    [Fact] void should_report_the_deletion() => _result.Removed.ShouldEqual(1);
    [Fact] void should_receipt_the_deleted_sources_from_the_prior_manifest() => _result.Receipt.Changes.Single(change => change.Kind == "delete").Sources.ShouldEqual<ArtifactSource>([new ArtifactSource("removed-id", "unknown", null)]);
    [Fact] void should_group_unknown_sources_last() => _result.Receipt.BySource[^1].Id.ShouldEqual("removed-id");
    [Fact] void should_record_the_exact_base_manifest() => _result.Receipt.Manifest.BaseSha256.ShouldEqual(_baseManifestHash);
    [Fact] void should_record_the_exact_result_manifest() => _result.Receipt.Manifest.Sha256.ShouldEqual(ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination)));
    [Fact] void should_not_include_the_manifest_among_artifact_changes() => _result.Receipt.Changes.ShouldNotContain(change => change.Path == ArtifactPublicationStorage.ManifestFileName);
}
