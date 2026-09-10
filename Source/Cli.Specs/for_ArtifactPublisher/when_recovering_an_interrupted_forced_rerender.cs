// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_recovering_an_interrupted_forced_rerender : given.an_artifact_publication
{
    const string UserContent = "user modified before force";
    string _manifestBefore = null!;
    bool _recovered;

    async Task Establish()
    {
        await Publish();
        await File.WriteAllTextAsync(FirstSourcePath(), UserContent);
        _manifestBefore = await File.ReadAllTextAsync(ArtifactPublicationStorage.ManifestPath(_destination));
    }

    async Task Because()
    {
        var interrupted = new ArtifactPublisher(new InterruptAfterFirstOperation());
        await Catch.Exception(() => interrupted.Publish(new(_plan, _destination, true), CancellationToken.None));
        _recovered = await new ArtifactPublisher().Recover(_destination, CancellationToken.None);
    }

    [Fact] void should_recover() => _recovered.ShouldBeTrue();
    [Fact] void should_restore_the_user_modified_file() => File.ReadAllText(FirstSourcePath()).ShouldEqual(UserContent);
    [Fact] void should_restore_the_exact_prior_manifest() => File.ReadAllText(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldEqual(_manifestBefore);

    sealed class InterruptAfterFirstOperation : IArtifactPublicationObserver
    {
        public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint)
        {
            if (checkpoint == ArtifactPublicationCheckpoint.OperationApplied)
            {
                throw new SimulatedInterruption();
            }
        }
    }

    sealed class SimulatedInterruption : Exception;
}
