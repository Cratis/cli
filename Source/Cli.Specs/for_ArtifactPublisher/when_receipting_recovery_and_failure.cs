// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_receipting_recovery_and_failure : given.an_artifact_publication
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_not_return_a_successful_receipt_when_publication_is_interrupted(int checkpoint)
    {
        ArtifactPublicationResult? result = null;
        var publisher = new ArtifactPublisher(new InterruptAt((ArtifactPublicationCheckpoint)checkpoint));

        var error = await Catch.Exception(async () => result = await publisher.Publish(new(_plan, _destination, false), CancellationToken.None));

        error.ShouldBeOfExactType<SimulatedInterruption>();
        result.ShouldBeNull();
    }

    [Fact]
    public async Task should_report_recovery_performed_inside_publish_and_receipt_only_the_new_operations()
    {
        var interrupted = new ArtifactPublisher(new InterruptAt(ArtifactPublicationCheckpoint.OperationApplied));
        var error = await Catch.Exception(() => interrupted.Publish(new(_plan, _destination, false), CancellationToken.None));
        error.ShouldBeOfExactType<SimulatedInterruption>();
        _plan.Artifacts.Any(artifact => File.Exists(ArtifactPath(artifact.RelativePath))).ShouldBeTrue();

        var result = await Publish();

        result.Recovered.ShouldBeTrue();
        result.Receipt.Manifest.BaseSha256.ShouldBeNull();
        result.Receipt.Changes.SequenceEqual(_plan.Artifacts.Select(artifact => new ArtifactPublicationChange(artifact.RelativePath, "write", null, artifact.Sha256))).ShouldBeTrue();
        Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
    }

    [Fact]
    public async Task should_not_return_a_receipt_when_canceled_before_publication()
    {
        ArtifactPublicationResult? result = null;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var error = await Catch.Exception(async () => result = await Publish(cancellationToken: cancellation.Token));

        error.ShouldBeOfExactType<OperationCanceledException>();
        result.ShouldBeNull();
        Directory.Exists(_destination).ShouldBeFalse();
    }

    [Fact]
    public async Task should_cancel_after_the_final_write_before_publishing_the_manifest_and_recover_normally()
    {
        ArtifactPublicationResult? canceledResult = null;
        using var cancellation = new CancellationTokenSource();
        var observer = new CancelOnFinalOperation(cancellation, _plan.Artifacts.Length);
        var publisher = new ArtifactPublisher(observer);

        var error = await Catch.Exception(async () => canceledResult = await publisher.Publish(new(_plan, _destination, false), cancellation.Token));

        observer.OperationsApplied.ShouldEqual(_plan.Artifacts.Length);
        cancellation.IsCancellationRequested.ShouldBeTrue();
        error.ShouldBeOfExactType<OperationCanceledException>();
        canceledResult.ShouldBeNull();
        _plan.Artifacts.All(artifact => File.Exists(ArtifactPath(artifact.RelativePath))).ShouldBeTrue();
        var journal = ArtifactPublicationStorage.ReadJournal(_destination);
        journal.ShouldNotBeNull();
        journal.BackupsComplete.ShouldBeTrue();
        journal.ManifestPublished.ShouldBeFalse();
        File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeFalse();

        var recoveredResult = await Publish();

        recoveredResult.Recovered.ShouldBeTrue();
        _plan.Artifacts.All(artifact =>
            File.Exists(ArtifactPath(artifact.RelativePath)) &&
            ArtifactPublicationStorage.Hash(ArtifactPath(artifact.RelativePath)) == artifact.Sha256).ShouldBeTrue();
        File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeTrue();
        Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
    }

    [Fact]
    public async Task should_not_return_a_receipt_for_an_unmanaged_collision()
    {
        ArtifactPublicationResult? result = null;
        Directory.CreateDirectory(Path.GetDirectoryName(FirstSourcePath())!);
        await File.WriteAllTextAsync(FirstSourcePath(), "unmanaged");

        var error = await Catch.Exception(async () => result = await Publish());

        error.ShouldBeOfExactType<UnsafeArtifactPublication>();
        result.ShouldBeNull();
        (await File.ReadAllTextAsync(FirstSourcePath())).ShouldEqual("unmanaged");
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

    sealed class CancelOnFinalOperation(CancellationTokenSource cancellation, int expectedOperations) : IArtifactPublicationObserver
    {
        public int OperationsApplied { get; private set; }

        public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint)
        {
            if (checkpoint == ArtifactPublicationCheckpoint.OperationApplied && ++OperationsApplied == expectedOperations)
            {
                cancellation.Cancel();
            }
        }
    }

    sealed class SimulatedInterruption : Exception;
}
