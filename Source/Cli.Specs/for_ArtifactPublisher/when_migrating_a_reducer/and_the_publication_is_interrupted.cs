// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_the_publication_is_interrupted : given.a_planned_reducer_migration
{
    string[] _before = [];
    bool _deletedBeforeRecovery;
    bool _recovered;

    async Task Establish()
    {
        _before = Files();
        var check = await new ArtifactPublisher().Check(MigrationRequest, CancellationToken.None);
        var interrupted = new ArtifactPublisher(new InterruptAfterOperations(check.Written + check.Removed));
        await Catch.Exception(() => interrupted.Publish(MigrationRequest, CancellationToken.None));
        _deletedBeforeRecovery = !File.Exists(ReducerFile);
    }

    async Task Because() => _recovered = await new ArtifactPublisher().Recover(_destination, CancellationToken.None);

    [Fact] void should_have_deleted_the_reducer_before_the_interruption() => _deletedBeforeRecovery.ShouldBeTrue();
    [Fact] void should_recover() => _recovered.ShouldBeTrue();
    [Fact] void should_restore_the_deleted_reducer() => File.ReadAllBytes(ReducerFile).ShouldEqual(_reducerBytes);
    [Fact] void should_restore_every_prior_file_byte_for_byte() => Files().ShouldEqual(_before);

    string[] Files() => [.. Snapshot(_destination).Where(entry => entry.StartsWith("file:", StringComparison.Ordinal))];
}
