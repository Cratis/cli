// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_the_reducer_changes_during_publication : given.a_planned_reducer_migration
{
    string[] _before = [];
    Exception _error = null!;

    void Establish() => _before = OtherFiles();

    async Task Because()
    {
        var publisher = new ArtifactPublisher(new EditAfterBackups(() => File.AppendAllText(ReducerFile, "// edited during publication\n")));
        _error = await Catch.Exception(() => publisher.Publish(MigrationRequest, CancellationToken.None));
    }

    [Fact] void should_refuse_the_removal() => ((UnsafeArtifactPublication)_error).DiagnosticCode.ShouldEqual(ArtifactPublicationRemovals.RefusalCode);
    [Fact] void should_keep_the_edited_reducer() => File.ReadAllText(ReducerFile).ShouldContain("edited during publication");
    [Fact] void should_roll_back_every_other_file() => OtherFiles().ShouldEqual(_before);
    [Fact] void should_leave_no_recovery_pending() => ArtifactPublicationStorage.HasPendingRecovery(_destination).ShouldBeFalse();

    string[] OtherFiles() => [.. Snapshot(_destination).Where(entry =>
        entry.StartsWith("file:", StringComparison.Ordinal) && !entry.StartsWith($"file:{_reducerPath}:", StringComparison.Ordinal))];

    sealed class EditAfterBackups(Action edit) : IArtifactPublicationObserver
    {
        public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint)
        {
            if (checkpoint == ArtifactPublicationCheckpoint.BackupsCompleted)
            {
                edit();
            }
        }
    }
}
