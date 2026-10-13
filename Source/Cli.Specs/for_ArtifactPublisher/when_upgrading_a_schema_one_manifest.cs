// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_upgrading_a_schema_one_manifest : given.an_artifact_publication
{
    ArtifactPublicationResult _result = null!;

    async Task Establish()
    {
        await Publish();
        var path = ArtifactPublicationStorage.ManifestPath(_destination);
        var legacy = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        legacy["schemaVersion"] = "1";
        foreach (var artifact in legacy["artifacts"]!.AsArray())
        {
            artifact!.AsObject().Remove("sources");
        }
        await File.WriteAllTextAsync(path, legacy.ToJsonString());
    }

    async Task Because() => _result = await Publish();

    [Fact] void should_upgrade_to_schema_two() => ArtifactPublicationStorage.ReadManifest(_destination)!.SchemaVersion.ShouldEqual("2");
    [Fact] void should_report_every_artifact_unchanged() => _result.Unchanged.ShouldEqual(_plan.Artifacts.Length);
    [Fact] void should_write_no_artifacts() => _result.Written.ShouldEqual(0);
    [Fact] void should_receipt_no_artifact_changes() => _result.Receipt.Changes.ShouldBeEmpty();
    [Fact] void should_report_the_manifest_rewrite() => _result.Receipt.Manifest.Sha256.ShouldNotEqual(_result.Receipt.Manifest.BaseSha256);
    [Fact] void should_preserve_every_artifact_hash() => _plan.Artifacts.All(artifact => ArtifactPublicationStorage.Hash(ArtifactPath(artifact.RelativePath)) == artifact.Sha256).ShouldBeTrue();

    [Fact]
    void should_store_every_planned_source()
    {
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        foreach (var artifact in _plan.Artifacts)
        {
            manifest.Artifacts.Single(managed => managed.Path == artifact.RelativePath).Sources.ShouldEqual(artifact.Sources.Select(id => id.ToString()));
        }
    }
}
