// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_a_manifest_has_no_owners : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _result = null!;
    string _stalePath = null!;

    async Task Establish()
    {
        var plan = await Plan("Tasks");
        await Publish(plan);
        _stalePath = "old-task.cs";
        await File.WriteAllTextAsync(Path.Combine(_destination, _stalePath), "old task\n");
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        var source = plan.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/CaptureTask.cs", StringComparison.Ordinal)).Sources;
        manifest = manifest with
        {
            Artifacts = [.. manifest.Artifacts.Select(artifact => artifact with { Owners = null }), new(_stalePath, ArtifactPublicationStorage.Hash(Path.Combine(_destination, _stalePath))) { Sources = [.. source.Select(id => id.ToString())] }]
        };
        await File.WriteAllTextAsync(ArtifactPublicationStorage.ManifestPath(_destination), ArtifactPublicationStorage.Serialize(manifest));
    }

    async Task Because() => _result = await Publish(await Plan("Tasks"));

    [Fact] void should_use_current_source_ownership() => _result.Removed.ShouldEqual(1);
    [Fact] void should_remove_the_owned_stale_file() => File.Exists(Path.Combine(_destination, _stalePath)).ShouldBeFalse();
}
