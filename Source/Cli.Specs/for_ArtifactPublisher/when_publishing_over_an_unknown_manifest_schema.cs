// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_publishing_over_an_unknown_manifest_schema : given.an_artifact_publication
{
    Exception _error = null!;

    void Establish()
    {
        Directory.CreateDirectory(_destination);
        var manifest = ArtifactManifest.From(_plan) with { SchemaVersion = "99" };
        ArtifactPublicationStorage.WriteDurable(
            ArtifactPublicationStorage.ManifestPath(_destination),
            ArtifactPublicationStorage.Serialize(manifest));
    }

    async Task Because() => _error = await Catch.Exception(() => Publish());

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_not_write_artifacts() => _plan.Artifacts.All(_ => !File.Exists(ArtifactPath(_.RelativePath))).ShouldBeTrue();
}
