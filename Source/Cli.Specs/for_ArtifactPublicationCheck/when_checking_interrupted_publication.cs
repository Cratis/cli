// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_interrupted_publication : given.a_publication_check
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_refuse_pending_recovery_without_touching_the_journal_staging_backups_or_artifacts(int checkpoint)
    {
        var interrupted = new ArtifactPublisher(new InterruptAt((ArtifactPublicationCheckpoint)checkpoint));
        var error = await Catch.Exception(() => interrupted.Publish(new(_plan, _destination, false), CancellationToken.None));
        error.ShouldBeOfExactType<SimulatedInterruption>();
        var before = Snapshot(_destination);

        var result = await Check();

        result.RecoveryPending.ShouldBeTrue();
        result.Refused.ShouldBeGreaterThan(0);
        result.Receipt.Changes.ShouldContain(_ => _.Kind == "refused" && _.Reason == "An interrupted publication needs recovery; run render without --check.");
        File.Exists(ArtifactPublicationStorage.JournalPath(_destination)).ShouldBeTrue();
        Directory.Exists(Path.Combine(ArtifactPublicationStorage.ControlPath(_destination), ArtifactPublicationStorage.StagingDirectoryName)).ShouldBeTrue();
        Snapshot(_destination).ShouldEqual(before);
    }

    [Fact]
    public async Task should_refuse_abandoned_staging_without_creating_a_journal_or_recovering()
    {
        ArtifactPublicationStorage.WriteDurable(ArtifactPublicationStorage.StagingPath(_destination, "abandoned.cs"), "staging bytes");
        var before = Snapshot(_destination);

        var result = await Check();

        result.RecoveryPending.ShouldBeTrue();
        result.Refused.ShouldEqual(1);
        Snapshot(_destination).ShouldEqual(before);
        File.Exists(ArtifactPublicationStorage.JournalPath(_destination)).ShouldBeFalse();
    }

    sealed class InterruptAt(ArtifactPublicationCheckpoint target) : IArtifactPublicationObserver
    {
        public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint)
        {
            if (checkpoint == target)
            {
                throw new SimulatedInterruption();
            }
        }
    }

    sealed class SimulatedInterruption : Exception;
}
