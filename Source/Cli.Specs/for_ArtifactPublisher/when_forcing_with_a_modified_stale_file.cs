// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_forcing_with_a_modified_stale_file : given.an_artifact_publication
{
    const string StalePath = "stale.cs";
    Exception _error = null!;

    async Task Establish()
    {
        await Publish();
        await File.WriteAllTextAsync(ArtifactPath(StalePath), "stale");
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        manifest = manifest with
        {
            Artifacts = [.. manifest.Artifacts, new ManagedArtifact(StalePath, ArtifactPublicationStorage.Hash(ArtifactPath(StalePath)))]
        };
        ArtifactPublicationStorage.WriteDurable(
            ArtifactPublicationStorage.ManifestPath(_destination),
            ArtifactPublicationStorage.Serialize(manifest));
        await File.WriteAllTextAsync(ArtifactPath(StalePath), "user modified stale");
    }

    async Task Because() => _error = await Catch.Exception(() => Publish(force: true));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_preserve_the_modified_stale_file() => File.ReadAllText(ArtifactPath(StalePath)).ShouldEqual("user modified stale");
}
