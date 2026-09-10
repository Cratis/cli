// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_rerendering_with_an_unchanged_stale_file : given.an_artifact_publication
{
    const string StalePath = "stale.cs";
    ArtifactPublicationResult _result = null!;

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
    }

    async Task Because() => _result = await Publish();

    [Fact] void should_remove_the_unchanged_stale_file() => File.Exists(ArtifactPath(StalePath)).ShouldBeFalse();
    [Fact] void should_report_one_removed_artifact() => _result.Removed.ShouldEqual(1);
}
