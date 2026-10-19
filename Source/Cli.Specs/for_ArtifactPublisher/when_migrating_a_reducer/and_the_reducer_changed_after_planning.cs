// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_the_reducer_changed_after_planning : given.a_planned_reducer_migration
{
    string[] _before = [];
    Exception _error = null!;

    void Establish()
    {
        File.AppendAllText(ReducerFile, "// edited after planning\n");
        _before = Snapshot(_destination);
    }

    async Task Because() => _error = await Catch.Exception(() => new ArtifactPublisher().Publish(MigrationRequest, CancellationToken.None));

    [Fact] void should_refuse_the_publication() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_report_the_unsafe_removal_code() => ((UnsafeArtifactPublication)_error).DiagnosticCode.ShouldEqual(ArtifactPublicationRemovals.RefusalCode);
    [Fact] void should_not_write_or_delete_anything() => Snapshot(_destination).ShouldEqual(_before);
}
