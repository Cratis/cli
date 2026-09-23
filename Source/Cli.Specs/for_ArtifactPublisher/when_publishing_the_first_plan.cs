// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_publishing_the_first_plan : given.an_artifact_publication
{
    ArtifactPublicationResult _result = null!;

    async Task Because() => _result = await Publish();

    [Fact] void should_report_the_receipt_version() => _result.Receipt.SchemaVersion.ShouldEqual("1");
    [Fact] void should_report_successful_publication() => _result.Receipt.Status.ShouldEqual("published");
    [Fact] void should_report_no_base_manifest() => _result.Receipt.Manifest.BaseSha256.ShouldBeNull();
    [Fact] void should_report_the_manifest_path() => _result.Receipt.Manifest.Path.ShouldEqual(".cratis-render.json");
    [Fact] void should_hash_the_exact_published_manifest() => _result.Receipt.Manifest.Sha256.ShouldEqual(ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination)));
    [Fact] void should_report_ordered_exact_created_artifacts() => _result.Receipt.Changes.SequenceEqual(_plan.Artifacts.Select(_ => new ArtifactPublicationChange(_.RelativePath, "write", null, _.Sha256))).ShouldBeTrue();
    [Fact] void should_not_report_recovery() => _result.Recovered.ShouldBeFalse();
    [Fact] void should_write_every_artifact() => _result.Written.ShouldEqual(_plan.Artifacts.Length);
    [Fact] void should_remove_nothing() => _result.Removed.ShouldEqual(0);
    [Fact] void should_write_the_exact_artifact_bytes() => File.ReadAllBytes(FirstSourcePath()).SequenceEqual(_plan.Artifacts.Single(_ => ArtifactPath(_.RelativePath) == FirstSourcePath()).Bytes).ShouldBeTrue();
    [Fact] void should_publish_the_ownership_manifest() => File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeTrue();
    [Fact] void should_publish_the_manifest_last_and_remove_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
}
