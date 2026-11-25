// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_recovering_ignore_only_control_state : given.an_artifact_publication
{
    Exception? _error;

    void Establish() => ArtifactPublicationStorage.EnsureControlDirectory(_destination);

    async Task Because() => _error = await Catch.Exception(() => _publisher.Recover(_destination, CancellationToken.None));

    [Fact] void should_not_treat_the_ignore_file_as_a_valid_journal() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_preserve_the_control_directory() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeTrue();
    [Fact] void should_preserve_the_ignore_file() => File.ReadAllText(Path.Combine(ArtifactPublicationStorage.ControlPath(_destination), ".gitignore")).ShouldEqual("*\n");
    [Fact] void should_not_create_a_manifest() => File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeFalse();
    [Fact] void should_not_create_a_journal() => File.Exists(ArtifactPublicationStorage.JournalPath(_destination)).ShouldBeFalse();
}
