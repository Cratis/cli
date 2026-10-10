// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_recovering_schema_one_publication : given.an_artifact_publication
{
    string _previousManifest = null!;
    bool _recovered;

    async Task Establish()
    {
        await Publish();
        var manifestPath = ArtifactPublicationStorage.ManifestPath(_destination);
        var legacy = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath))!;
        legacy["schemaVersion"] = "1";
        foreach (var artifact in legacy["artifacts"]!.AsArray())
        {
            artifact!.AsObject().Remove("sources");
        }
        _previousManifest = legacy.ToJsonString();
        await File.WriteAllTextAsync(manifestPath, _previousManifest);
        await File.WriteAllTextAsync(FirstSourcePath(), "user modified before force");
        var interrupted = new ArtifactPublisher(new InterruptAfterFirstOperation());
        var error = await Catch.Exception(() => interrupted.Publish(new(_plan, _destination, true), CancellationToken.None));
        error.ShouldBeOfExactType<SimulatedInterruption>();
        var journalPath = ArtifactPublicationStorage.JournalPath(_destination);
        var journal = JsonNode.Parse(await File.ReadAllTextAsync(journalPath))!;
        journal["nextManifest"] = legacy;
        await File.WriteAllTextAsync(journalPath, journal.ToJsonString());
        ArtifactPublicationStorage.ReadJournal(_destination)!.NextManifest.Artifacts.All(artifact => artifact.Sources.Count == 0).ShouldBeTrue();
    }

    async Task Because() => _recovered = await _publisher.Recover(_destination, CancellationToken.None);

    [Fact] void should_recover_the_old_journal() => _recovered.ShouldBeTrue();
    [Fact] void should_restore_the_modified_bytes() => File.ReadAllText(FirstSourcePath()).ShouldEqual("user modified before force");
    [Fact] void should_restore_the_exact_schema_one_manifest() => File.ReadAllText(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldEqual(_previousManifest);
    [Fact] void should_remove_the_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();

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
