// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_stale_semantic_sources : given.a_publication_check
{
    [Theory]
    [InlineData(false, "delete")]
    [InlineData(true, "refused")]
    public async Task should_report_the_prior_manifest_sources(bool modified, string kind)
    {
        await Publish();
        AddStale("stale.cs", modified);
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.ManifestPath(_destination), ArtifactPublicationStorage.Serialize(manifest with
        {
            Artifacts = [.. manifest.Artifacts.Select(artifact => artifact.Path == "stale.cs" ? artifact with { Sources = ["removed-declaration"] } : artifact)]
        }));
        var before = Snapshot(_destination);

        var result = await Check();

        var stale = result.Receipt.Changes.Single(change => change.Path == "stale.cs");
        stale.Kind.ShouldEqual(kind);
        stale.Sources.ShouldEqual<ArtifactSource>([new ArtifactSource("removed-declaration", "unknown", null)]);
        var grouped = result.Receipt.BySource.Single(source => source.Id == "removed-declaration");
        grouped.Kind.ShouldEqual("unknown");
        grouped.Address.ShouldBeNull();
        grouped.Paths.ShouldEqual<string>(["stale.cs"]);
        Snapshot(_destination).ShouldEqual(before);
    }
}
