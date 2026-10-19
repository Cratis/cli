// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class with_a_classic_reducer : given.a_classic_reducer_destination
{
    void Establish() => _settings.MigrateReducer = [ReadModel];

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_plan_the_removal_of_the_classic_reducer() => _plans.Single().Artifacts!.RequiredRemovals.Single().RelativePath.ShouldEqual(_reducerPath);
    [Fact] void should_delete_the_classic_reducer_file() => File.Exists(ReducerFile).ShouldBeFalse();
    [Fact] void should_no_longer_own_the_classic_reducer() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Select(artifact => artifact.Path).ShouldNotContain(_reducerPath);
    [Fact] void should_persist_version_two_of_the_reducer_configuration() => File.ReadAllText(_configuration).ShouldContain("\"version\":2");
    [Fact] void should_pin_the_previous_reducer_path() => File.ReadAllText(_configuration).ShouldContain(_reducerPath);
    [Fact] void should_leave_no_recovery_journal() => ArtifactPublicationStorage.HasPendingRecovery(_destination).ShouldBeFalse();
}
