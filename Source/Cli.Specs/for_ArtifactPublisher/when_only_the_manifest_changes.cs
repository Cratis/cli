// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_only_the_manifest_changes : given.an_artifact_publication
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_manifest_changes_without_inventing_artifact_operations(bool missingStaleArtifact)
    {
        await Publish();
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        manifest = missingStaleArtifact
            ? manifest with { Artifacts = [.. manifest.Artifacts, new ManagedArtifact("already-absent.cs", new string('a', 64))] }
            : manifest with { TargetVersion = "previous-target-version" };
        ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.ManifestPath(_destination), ArtifactPublicationStorage.Serialize(manifest));
        var before = ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination));

        var result = await Publish();

        result.Written.ShouldEqual(0);
        result.Removed.ShouldEqual(0);
        result.Unchanged.ShouldEqual(_plan.Artifacts.Length);
        result.Receipt.Changes.ShouldBeEmpty();
        result.Receipt.Manifest.BaseSha256.ShouldEqual(before);
        result.Receipt.Manifest.Sha256.ShouldEqual(ArtifactPublicationStorage.Hash(ArtifactPublicationStorage.ManifestPath(_destination)));
        result.Receipt.Manifest.Sha256.ShouldNotEqual(before);
        File.Exists(ArtifactPath("already-absent.cs")).ShouldBeFalse();
    }
}
