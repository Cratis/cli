// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_a_removal_backup_was_tampered_with : given.a_planned_reducer_migration
{
    Exception _error = null!;

    async Task Establish()
    {
        await Catch.Exception(() => new ArtifactPublisher(new InterruptAfterOperations(1)).Publish(MigrationRequest, CancellationToken.None));
        await File.WriteAllTextAsync(ArtifactPublicationStorage.BackupPath(_destination, _reducerPath), "tampered");
    }

    async Task Because() => _error = await Catch.Exception(() => new ArtifactPublisher().Recover(_destination, CancellationToken.None));

    [Fact] void should_refuse_recovery() => ((UnsafeArtifactPublication)_error).DiagnosticCode.ShouldEqual(ArtifactPublicationRemovals.RefusalCode);
    [Fact] void should_not_restore_the_tampered_content() => File.Exists(ReducerFile).ShouldBeFalse();
    [Fact] void should_keep_the_journal_for_a_later_recovery() => ArtifactPublicationStorage.HasPendingRecovery(_destination).ShouldBeTrue();
}
