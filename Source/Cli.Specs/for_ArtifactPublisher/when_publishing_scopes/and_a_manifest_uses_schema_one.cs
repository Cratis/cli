// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_a_manifest_uses_schema_one : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _result = null!;

    async Task Establish()
    {
        await Publish(await Plan());
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        manifest = manifest with { SchemaVersion = "1", Artifacts = [.. manifest.Artifacts.Select(artifact => artifact with { Sources = [], Owners = null })] };
        File.WriteAllText(ArtifactPublicationStorage.ManifestPath(_destination), ArtifactPublicationStorage.Serialize(manifest));
        File.Delete(_lookupFile);
    }

    async Task Because() => _result = await Publish(await Plan("Projects.Registration"));

    [Fact] void should_delete_nothing_without_semantic_ownership() => _result.Removed.ShouldEqual(0);
    [Fact] void should_preserve_the_legacy_stale_file() => File.Exists(Path.Combine(_destination, "Projects/Registration/ProjectLookup/ProjectLookup.cs")).ShouldBeTrue();
}
