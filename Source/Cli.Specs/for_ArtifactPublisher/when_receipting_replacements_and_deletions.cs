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
            ArtifactPublicationStorage.Serialize(manifest with { Artifacts = [.. manifest.Artifacts, new ManagedArtifact(StalePath, _before[StalePath])] }));
        _baseManifestHash = ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination));
        await File.WriteAllTextAsync(_file, Source.Replace("Screenplay", "Updated project", StringComparison.Ordinal));
        _plan = (await Plan()).Artifacts!;
        _expected =
        [
            .. _plan.Artifacts.Where(artifact => artifact.Sha256 != _before[artifact.RelativePath])
                .Select(artifact => new ArtifactPublicationChange(artifact.RelativePath, "write", _before[artifact.RelativePath], artifact.Sha256)),
            new(StalePath, "delete", _before[StalePath], null)
        ];
    }

    async Task Because() => _result = await Publish();

    [Fact] void should_exercise_actual_replacements() => _expected.Length.ShouldBeGreaterThan(1);
    [Fact] void should_report_plan_ordered_writes_then_the_owned_deletion() => _result.Receipt.Changes.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_report_only_changed_artifacts() => _result.Written.ShouldEqual(_expected.Length - 1);
    [Fact] void should_report_the_deletion() => _result.Removed.ShouldEqual(1);
    [Fact] void should_record_the_exact_base_manifest() => _result.Receipt.Manifest.BaseSha256.ShouldEqual(_baseManifestHash);
    [Fact] void should_record_the_exact_result_manifest() => _result.Receipt.Manifest.Sha256.ShouldEqual(ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination)));
    [Fact] void should_not_include_the_manifest_among_artifact_changes() => _result.Receipt.Changes.ShouldNotContain(change => change.Path == ArtifactPublicationStorage.ManifestFileName);
}
