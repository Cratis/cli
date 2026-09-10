// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_recovering_an_interrupted_first_publication : given.an_artifact_publication
{
    Exception _interruption = null!;
    bool _recovered;

    async Task Because()
    {
        var interrupted = new ArtifactPublisher(new InterruptAfterFirstOperation());
        _interruption = await Catch.Exception(() => interrupted.Publish(new(_plan, _destination, false), CancellationToken.None));
        _recovered = await new ArtifactPublisher().Recover(_destination, CancellationToken.None);
    }

    [Fact] void should_observe_the_interruption() => _interruption.ShouldBeOfExactType<SimulatedInterruption>();
    [Fact] void should_recover() => _recovered.ShouldBeTrue();
    [Fact] void should_remove_every_partially_created_artifact() => _plan.Artifacts.All(_ => !File.Exists(ArtifactPath(_.RelativePath))).ShouldBeTrue();
    [Fact] void should_restore_the_absent_manifest() => File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeFalse();
    [Fact] void should_remove_the_journal_and_staging_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();

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
