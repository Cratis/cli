// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_publishing_the_first_plan : given.an_artifact_publication
{
    ArtifactPublicationResult _result = null!;

    async Task Because() => _result = await Publish();

    [Fact] void should_write_every_artifact() => _result.Written.ShouldEqual(_plan.Artifacts.Length);
    [Fact] void should_remove_nothing() => _result.Removed.ShouldEqual(0);
    [Fact] void should_write_the_exact_artifact_bytes() => File.ReadAllBytes(FirstSourcePath()).SequenceEqual(_plan.Artifacts.Single(_ => ArtifactPath(_.RelativePath) == FirstSourcePath()).Bytes).ShouldBeTrue();
    [Fact] void should_publish_the_ownership_manifest() => File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeTrue();
    [Fact] void should_publish_the_manifest_last_and_remove_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
}
