// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_rendering_again_without_the_option : given.a_classic_reducer_destination
{
    string[] _migrated = [];

    async Task Establish()
    {
        _settings.MigrateReducer = [ReadModel];
        (await Capture()).ExitCode.ShouldEqual(ExitCodes.Success);
        _migrated = Snapshot(_destination);
        _settings.MigrateReducer = [];
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_read_the_version_two_configuration_and_plan_no_removals() => _plans[^1].Artifacts!.RequiredRemovals.ShouldBeEmpty();
    [Fact] void should_keep_every_byte() => Snapshot(_destination).ShouldEqual(_migrated);
}
